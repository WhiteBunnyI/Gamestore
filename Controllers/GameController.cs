using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Gamestore.Controllers;

[Route("api/games")]
public class GameController : AppControllerBase
{
    protected override string Entity => "Игра";

    private DeveloperService _developerService;
    private PublisherService _publisherService;
    private GenreService _genreService;
    private UserService _userService;
    private GameService _gameService;

    public GameController(DbCtx db, ILogger<GameController> logger,
        PublisherService publisherService, GameService gameService, 
        GenreService genreService, DeveloperService developerService,
        UserService userService) : base(db, logger)
    {
        _developerService = developerService;
        _publisherService = publisherService;
        _genreService = genreService;
        _gameService = gameService;
        _userService = userService;
    }

    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IResult> AddGame([FromBody] Game.GameDto gameDto)
    {
        var game = new Game
        {
            Title = gameDto.Title,
            Description = gameDto.Description,
            DateRelease = DateOnly.FromDateTime(DateTime.UtcNow),
            SystemRequired = gameDto.SystemRequired,
            Price = gameDto.Price,
            PublisherId = gameDto.PublisherId
        };

        if (await _publisherService.Get(game.PublisherId) is null)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE("Издатель"));

        var added = await _gameService.Add(game);

        if (added == 0)
            return Results.Conflict(CONFLICT_AUTO_MESSAGE);

        game = await _ctx.Games.AsNoTracking()
            .Where(g => g.Title.Equals(game.Title))
            .FirstOrDefaultAsync();

        if (game == null)
            return Results.Problem("Не удалось добавить разработчиков и жанры к игре. Добавьте их вручную");

        string textError = string.Empty;
        List<Task<int>> tasks = new List<Task<int>>();
        List<int> counts = [gameDto.Developers.Count, gameDto.Genres.Count];

        if (gameDto.Developers.Count > 0)
        {
            /* 
            StringBuilder sb = new StringBuilder();
            foreach (var devChunk in gameDto.Developers.Chunk(500))
            {
                foreach (var dev in devChunk)
                {
                    if (sb.Length != 0)
                        sb.Append(", ");
                    sb.Append($"({game.Id}, {dev})");
                }

                tasks.Add(_ctx.Database.ExecuteSqlInterpolatedAsync($@"
                            INSERT INTO gamestore.game_developer (game_id, developer_id)
                            SELECT s.devId, s.gameId 
                            FROM (VALUES {sb.ToString()})
                            WHERE EXIST (SELECT 1 FROM gamestore.developer d WHERE d.id = s.devId)
                            ON CONFLICT (game_id, developer_id) DO NOTHING;"));
            }
            */

            //Validating
            gameDto.Developers = await _ctx.Developers
                .Where(d => gameDto.Developers.Contains(d.Id))
                .Select(d => d.Id)
                .ToArrayAsync();

            var devsList = gameDto.Developers
                .Select(d => new GameDeveloper { DeveloperId = d, GameId = game.Id })
                .ToList();

            tasks.Add(_ctx.GameDevelopers.UpsertRange(devsList)
                .On(gd => new { gd.DeveloperId, gd.GameId })
                .NoUpdate()
                .RunAsync());
        }

        if (gameDto.Genres.Count > 0)
        {
            gameDto.Genres = await _ctx.Genres
                .Where(g => gameDto.Genres.Contains(g.Id))
                .Select(g => g.Id)
                .ToArrayAsync();

            var genList = gameDto.Genres
                .Select(g => new GameGenre { GenreId = g, GameId = game.Id })
                .ToList();

            tasks.Add(_ctx.GameGenres.UpsertRange(genList)
                .On(gg => new { gg.GenreId, gg.GameId })
                .NoUpdate()
                .RunAsync());
        }

        await Task.WhenAll(tasks);


        added = tasks[0].Result;
        if (added == 0)
            textError += "Не удалось добавить разработчиков к игре! Проверьте id и добавьте их вручную! ";

        else if (added < counts[0])
            textError += "Не удалось добавить некоторых разработчиков! Проверьте id и добавьте их вручную! ";


        added = tasks[1].Result;
        if (added == 0)
            textError += "Не удалось добавить жанры к игре! Проверьте id и добавьте их вручную! ";

        else if (added < counts[1])
            textError += "Не удалось добавить некоторые жанры! Проверьте id и добавьте их вручную! ";

        if(textError.Length != 0)
            return Results.Ok(textError);


        return Results.Ok();
    }

    private async Task<Game?> GetGameObj(int? id, string? title)
    {
        Game? game = null;

        if (id != null)
            game ??= await _gameService.Get(id.Value);

        if (title != null)
            game ??= await _gameService.Get(title);

        return game;
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetGame(int? id, string? title)
    {
        Game? game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok(game);
    }

    [HttpPost("buy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> BuyGame(string login, int gameId)
    {
        _logger.LogInformation("Пользователь {Login} хочет купить игру {GameId}", login, gameId);

        using var transaction = await _ctx.Database.BeginTransactionAsync();

        if (await _userService.Get(login) is not User user)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE($"Пользователь {login}"));

        if (await _gameService.Get(gameId) is not Game game)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE($"Игра с id: {gameId}"));

        int gameAdded = await _ctx.GameUsers
            .Upsert(new GameUser { UserId = user.Id, GameId = gameId, Price = game.Price, DatePurchase = DateOnly.FromDateTime(DateTime.UtcNow) })
            .On(gu => new { gu.UserId, gu.GameId })
            .NoUpdate()
            .RunAsync();

        if (gameAdded == 0)
            return Results.Conflict($"Пользователь {login} уже приобрел игру {game.Title}");

        var check = await _ctx.Users
            .Where(u => u.Login == login && u.Wallet >= game.Price)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Wallet, u => u.Wallet - game.Price));

        if (check == 0)
            return Results.BadRequest($"На балансе пользователя {login} недостаточно средств!");

        await transaction.CommitAsync();
        return Results.Ok($"Пользователь {login} успешно приобрел игру {game.Title}!");
    }

    [HttpDelete("delete")]
    public async Task<IResult> DeleteGame(int id)
    {
        //Удаляем ссылки со смежных таблиц
        //Удаляем саму игру



        throw new NotImplementedException();
    }


    [HttpPost("add-developers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddGameDevelopers(int? gameId, string? title, [FromBody] List<int> developersId)
    {
        var game = await GetGameObj(gameId, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var gameDevs = developersId.Select(id => new GameDeveloper() { DeveloperId = id, GameId = game.Id }).ToList();

        int added = await _ctx.GameDevelopers
            .UpsertRange(gameDevs)
            .On(gd => new { gd.DeveloperId, gd.GameId })
            .NoUpdate()
            .RunAsync();

        if (added == 0)
            return Results.BadRequest("Разработчики не были добавлены! Проверьте id");

        if (added < gameDevs.Count)
            return Results.BadRequest("Не все разработчики были добавлены! Проверьте id");

        return Results.Ok();
    }

    [HttpGet("get-developers")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGameDevelopers(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var devs = await _ctx.GameDevelopers
            .Where(gd => gd.GameId == game.Id)
            .Include(gd => gd.Developer)
            .Select(gd => gd.Developer.Name)
            .ToListAsync();

        if (devs == null)
            return Results.NotFound("У игры нет разработчиков");

        return Results.Ok(devs);
    }

    [HttpGet("get-genres")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGameGenres(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var genres = await _ctx.GameGenres
            .Where(gg => gg.GameId == game.Id)
            .Include(gg => gg.Genre)
            .Select(gg => gg.Genre.Name)
            .ToListAsync();

        if (genres == null)
            return Results.NotFound("У игры нет жанров");

        return Results.Ok(genres);
    }

    [HttpGet("get-publisher")]
    [ProducesResponseType(typeof(Publisher), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGamePublisher(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        await _ctx.Entry(game)
            .Reference(g => g.Publisher)
            .LoadAsync();

        if (game.Publisher == null)
        {
            _logger.LogError("У игры {Title} нет издателя!", game.Title);
            return Results.NotFound("У игры нет издателя");
        }

        return Results.Ok(game.Publisher);
    }

    [HttpGet("get-version-history")]
    [ProducesResponseType(typeof(IEnumerable<GameVersion>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGameVersionHistory(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        await _ctx.Entry(game)
            .Collection(g => g.GameVersions)
            .LoadAsync();

        if (game.GameVersions == null)
            return Results.NotFound("У игры нет истории версий");

        return Results.Ok(game.GameVersions);
    }

    [HttpGet("get-version")]
    [ProducesResponseType(typeof(GameVersion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGameVersion(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var version = await _ctx.Entry(game)
            .Collection(g => g.GameVersions)
            .Query()
            .OrderByDescending(gg => gg.DateRelease)
            .FirstOrDefaultAsync();

        if (version == null)
            return Results.NotFound("У игры нет истории версий");

        return Results.Ok(version);
    }


}
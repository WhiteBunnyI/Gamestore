using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;

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

    [Authorize(Roles = "Admin")]
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
            await Task.Run(async () =>
            {
                var developers = await _developerService.Get(gameDto.Developers);
                return await _gameService.AddDevelopers(game, developers);
            });
        }

        if (gameDto.Genres.Count > 0)
        {
            await Task.Run(async () =>
            {
                var genres = await _genreService.Get(gameDto.Genres);
                return await _gameService.AddGenres(game, genres);
            });
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

        if (textError.Length != 0)
            return Results.Ok(textError);


        return Results.Ok(SUCCESS_ADDED_AUTO_MESSAGE);
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

    [HttpGet("get/{page}")]
    public async Task<IResult> GetGame(int page)
    {
        int limitPerPage = 12;
        return Results.Ok(await _gameService.Get(limitPerPage, limitPerPage * (page - 1)));
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

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete")]
    public async Task<IResult> DeleteGame(int id)
    {
        //Удаляем ссылки со смежных таблиц
        //Удаляем саму игру

        using var transaction = await _ctx.Database.BeginTransactionAsync();

        var game = await _gameService.Get(id);
        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        List<Task> tasks = new List<Task>()
        {
            _gameService.RemoveAllDevelopers(game),
            _gameService.RemoveAllGenres(game),
            _gameService.RemoveAllVersions(game)
        };

        await Task.WhenAll(tasks);

        int removed = 0;
        try
        {
            removed = await _gameService.Delete(game.Id);
        }
        catch (DbException ex)
        when (ex is Npgsql.PostgresException pgEx && pgEx.SqlState.Equals(Npgsql.PostgresErrorCodes.ForeignKeyViolation))
        {
            return Results.BadRequest("Данная игра содержится в библиотеках пользователей");
        }

        if (removed == 0)
            return Results.BadRequest("Данная игра содержится в библиотеках пользователей");

        await transaction.CommitAsync();

        return Results.Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add-developers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddGameDevelopers(int? gameId, string? title, [FromBody] List<int> developersId)
    {
        var game = await GetGameObj(gameId, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var devList = await _developerService.Get(developersId);

        int added = await _gameService.AddDevelopers(game, devList);

        if (added == 0)
            return Results.BadRequest("Разработчики не были добавлены! Проверьте id");

        if (added < developersId.Count)
            return Results.BadRequest("Не все разработчики были добавлены! Проверьте id");

        return Results.Ok(SUCCESS_ADDED_EXACT_MESSAGE("Разработчик"));
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

        var devs = await _gameService.GetDevelopers(game);

        if (devs == null)
            return Results.NotFound("У игры нет разработчиков");

        return Results.Ok(devs.Select(d => d.Id));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add-genres")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddGameGenres(int? id, string? title, [FromBody] List<int> genreIds)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        var genList = await _genreService.Get(genreIds);
        int added = await _gameService.AddGenres(game, genList);
        if (added == 0)
            return Results.BadRequest("Жанры не были добавлены! Проверьте id");

        if (added < genreIds.Count)
            return Results.BadRequest("Не все жанры были добавлены! Проверьте id");

        return Results.Ok(SUCCESS_ADDED_EXACT_MESSAGE("Жанр"));
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

        var genres = await _gameService.GetGenres(game);

        if (genres == null)
            return Results.NotFound("У игры нет жанров");

        return Results.Ok(genres.Select(g => g.Name));
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


    [Authorize(Roles = "Admin")]
    [HttpPost("add-version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddGameVersion(int? id, string? title, GameVersion.VersionDto dto)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        GameVersion gameVersion = new GameVersion()
        {
            GameId = game.Id,
            Description = dto.Description,
            DateRelease = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await _gameService.AddVersion(gameVersion);

        return Results.Ok();
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
            return Results.NotFound(NOT_FOUND_EXACT_MESSAGE("История"));

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
            return Results.NotFound(NOT_FOUND_EXACT_MESSAGE("История"));

        return Results.Ok(version);
    }


    [Authorize(Roles = "User")]
    [HttpPost("buy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> BuyGame([FromBody] List<int> gameIds)
    {
        var login = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(login))
            return Results.Unauthorized();

        _logger.LogInformation("Пользователь {Login} хочет купить игры {GameIds}", login, gameIds);

        using var transaction = await _ctx.Database.BeginTransactionAsync();

        if (await _userService.Get(login) is not User user)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE($"Пользователь {login}"));


        //Переделать под мн-во игр - сделать проверки и реализовать
        var foundGames = await _gameService.Get(gameIds);
        if (foundGames == null)
            return Results.BadRequest("Ошибка с соединением с бд");

        float priceSum = 0;
        List<GameUser> gameUsers = new List<GameUser>(foundGames.Count);
        foreach(var game in foundGames)
        {
            GameUser gameUser = new GameUser
            {
                UserId = user.Id,
                GameId = game.Id,
                Price = game.Price,
                DatePurchase = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            gameUsers.Add(gameUser);
            priceSum += game.Price;
        }

        int gameAdded = await _gameService.AddGameToUser(gameUsers);

        if (gameAdded == 0)
            return Results.Conflict($"Пользователь {login} уже приобрел данные игры");

        var check = await _ctx.Users
            .Where(u => u.Login == login && u.Wallet >= priceSum)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Wallet, u => u.Wallet - priceSum));

        if (check == 0)
            return Results.BadRequest($"На балансе пользователя {login} недостаточно средств!");

        await transaction.CommitAsync();

        _logger.LogInformation("Пользователь {Login} успешно купил игры {GameIds}", login, gameIds);

        return Results.Ok($"Пользователь {login} успешно приобрел игры {gameIds}!");
    }

    [Authorize]
    [HttpGet("get-library")]
    [ProducesResponseType(typeof(ICollection<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUserLibrary(int userId)
    {
        User? user = await _userService.Get(userId);
        if (user == null)
            return Results.NotFound(NOT_FOUND_EXACT_MESSAGE("Пользователь"));

        var gameList = await _gameService.GetUserGames(user);

        return Results.Ok(gameList.Select(g => g.Id));
    }
}
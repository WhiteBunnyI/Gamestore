using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Controllers;

[Route("api/games")]
public class GameController : AppControllerBase
{
    public GameController(DbCtx db, ILogger<GameController> logger) : base(db, logger)
    {
    }

    private async Task<Game?> GetGameObj(int? id, string? title)
    {
        Game? game = null;

        if (id != null)
            game ??= await _ctx.Games.FindAsync(id);

        if (title != null)
            game ??= await _ctx.Games.FirstOrDefaultAsync(g => g.Title == title);

        return game;
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetGame(int? id, string? title)
    {
        Game? game = await GetGameObj(id, title);
        
        if(game == null)
            return Results.BadRequest();
        
        return Results.Ok(game);
    }

    [HttpPost("buy-game")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> BuyGame(string login, int gameId)
    {
        _logger.LogInformation("Пользователь {Login} хочет купить игру {GameId}", login, gameId);

        using var transaction = await _ctx.Database.BeginTransactionAsync();

        if (await _ctx.Users.FirstOrDefaultAsync(u => u.Login == login) is not User user)
            return Results.BadRequest($"Пользователь с логином {login} не найден!");

        if (await _ctx.Games.FindAsync(gameId) is not Game game)
            return Results.BadRequest($"Игры с id: {gameId} не найдено!");

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

    [HttpGet("get-genres")]
    [ProducesResponseType(typeof(IEnumerable<Genre>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGameGenres(int? id, string? title)
    {
        var game = await GetGameObj(id, title);

        if (game == null)
            return Results.BadRequest();

        var genres = await _ctx.GameGenres
            .Where(gg => gg.GameId == game.Id)
            .Include(gg => gg.Genre)
            .Select(gg => gg.Genre)
            .ToListAsync();

        if (genres == null)
            return Results.NotFound();

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
            return Results.BadRequest();

        await _ctx.Entry(game)
            .Reference(g => g.Publisher)
            .LoadAsync();

        if (game.Publisher == null)
            return Results.NotFound();

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
            return Results.BadRequest();

        await _ctx.Entry(game)
            .Collection(g => g.GameVersions)
            .LoadAsync();

        if (game.GameVersions == null)
            return Results.NotFound();

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
            return Results.BadRequest();

        var version = await _ctx.Entry(game)
            .Collection(g => g.GameVersions)
            .Query()
            .OrderByDescending(gg => gg.DateRelease)
            .FirstOrDefaultAsync();

        if (version == null)
            return Results.NotFound();

        return Results.Ok(version);
    }
}
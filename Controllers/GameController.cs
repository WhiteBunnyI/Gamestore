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

    [HttpGet("get")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetGame(int? id, string? title)
    {
        Game? game = null;

        if(id != null)
            game ??= await _ctx.Games.FindAsync(id);

        if(title != null)
            game ??= await _ctx.Games.FirstOrDefaultAsync(g => g.Title == title);
        
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
}
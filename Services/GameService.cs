using Gamestore.Models;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Services;

public class GameService
{
    private DbCtx _ctx;

    public GameService(DbCtx db)
    {
        _ctx = db;
    }

    public async Task<int> Add(Game game) => await _ctx.Games
        .Upsert(game)
        .On(g => g.Title)
        .NoUpdate()
        .RunAsync();

    public async Task<Game?> Get(int id) => await _ctx.Games.FindAsync(id);
    public async Task<Game?> Get(string title) => await _ctx.Games
        .Where(g => g.Title == title)
        .FirstOrDefaultAsync();

    public async Task<int> Delete(int id) => await _ctx.Games
        .Where(g => g.Id == id)
        .ExecuteDeleteAsync();
}

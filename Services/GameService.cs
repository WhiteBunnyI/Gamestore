using Gamestore.Models;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Services;

public class GameService
{
    private DbCtx _ctx;

    private ILogger _logger;
    public GameService(DbCtx db, ILogger<GameService> logger)
    {
        _ctx = db;
        _logger = logger;
    }


    public async Task<int> Add(Game game) => await _ctx.Games
        .Upsert(game)
        .On(g => g.Title)
        .NoUpdate()
        .RunAsync();

    public async Task<Game?> Get(int id)
    {
        return await _ctx.Games.FindAsync(id);
    }

    public async Task<Game?> Get(string title)
    {
        return await _ctx.Games
            .Where(g => g.Title == title)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Game>> Get(ICollection<int> ids)
    {
        return await _ctx.Games
            .Where(g => ids.Contains(g.Id))
            .ToListAsync();
    }

    public async Task<List<Game>> Get(int limit, int offset)
    {
        //Не очень хорошо, лучше использовать пагинацию по индексу
        //То т.к. таблица небольшая, можно и так
        return await _ctx.Games
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetCount()
    {
        return await _ctx.Games.CountAsync();
    }

    public async Task<int> Delete(int id)
    {
        return await _ctx.Games
            .Where(g => g.Id == id)
            .ExecuteDeleteAsync();
    }


    public async Task AddVersion(GameVersion gameVersion)
    {
        await _ctx.GameVersions.AddAsync(gameVersion);
        await _ctx.SaveChangesAsync();
    }

    public async Task<List<GameVersion>> GetVersionsList(Game game)
    {
        return await _ctx.GameVersions
            .Where(gv => gv.GameId == game.Id)
            .ToListAsync();
    }

    public async Task<int> RemoveAllVersions(Game game)
    {
        return await _ctx.GameVersions
            .Where(gv => gv.GameId == game.Id)
            .ExecuteDeleteAsync();
    }


    public async Task<int> AddDevelopers(Game game, IEnumerable<Developer> developers)
    {
        var devsList = developers
            .Select(d => new GameDeveloper { GameId = game.Id, DeveloperId = d.Id })
            .ToList();

        return await _ctx.GameDevelopers
            .UpsertRange(devsList)
            .On(gd => new { gd.GameId, gd.DeveloperId })
            .NoUpdate()
            .RunAsync();
    }

    public async Task<List<Developer>> GetDevelopers(Game game)
    {
        return await _ctx.GameDevelopers
            .Where(gd => gd.GameId == game.Id)
            .Include(gd => gd.Developer)
            .Select(gd => gd.Developer)
            .ToListAsync();
    }

    public async Task<int> RemoveDevelopers(Game game, IEnumerable<Developer> developers)
    {
        var ids = developers.Select(d => d.Id).ToList();

        return await _ctx.GameDevelopers
            .Where(gd => gd.GameId == game.Id && ids.Contains(gd.DeveloperId))
            .ExecuteDeleteAsync();
    }

    public async Task<int> RemoveAllDevelopers(Game game)
    {
        return await _ctx.GameDevelopers
            .Where(gd => gd.GameId == game.Id)
            .ExecuteDeleteAsync();
    }


    public async Task<int> AddGenres(Game game, IEnumerable<Genre> genres)
    {
        var genresList = genres
            .Select(g => new GameGenre { GameId = game.Id, GenreId = g.Id })
            .ToList();

        return await _ctx.GameGenres
            .UpsertRange(genresList)
            .On(gd => new { gd.GameId, gd.GenreId })
            .NoUpdate()
            .RunAsync();
    }

    public async Task<List<Genre>> GetGenres(Game game)
    {
        return await _ctx.GameGenres
            .Where(gg => gg.GameId == game.Id)
            .Include(gg => gg.Genre)
            .Select(gg => gg.Genre)
            .ToListAsync();
    }

    public async Task<int> RemoveGenres(Game game, IEnumerable<Genre> genres)
    {
        var ids = genres.Select(g => g.Id).ToList();

        return await _ctx.GameGenres
            .Where(gg => gg.GameId == game.Id && ids.Contains(gg.GenreId))
            .ExecuteDeleteAsync();
    }

    public async Task<int> RemoveAllGenres(Game game)
    {
        return await _ctx.GameGenres
            .Where(gg => gg.GameId == game.Id)
            .ExecuteDeleteAsync();
    }


    public async Task<int> AddGameToUser(GameUser data)
    {
        return await _ctx.GameUsers
            .Upsert(data)
            .On(gu => new { gu.GameId, gu.UserId })
            .NoUpdate()
            .RunAsync();
    }

    public async Task<int> AddGameToUser(ICollection<GameUser> data)
    {
        return await _ctx.GameUsers
            .UpsertRange(data)
            .On(gu => new { gu.GameId, gu.UserId })
            .NoUpdate()
            .RunAsync();
    }

    public async Task<List<Game>> GetUserGames(User user)
    {
        return await _ctx.GameUsers
            .Where(gu => gu.UserId == user.Id)
            .Include(gu => gu.Game)
            .Select(gu => gu.Game)
            .ToListAsync();
    }

    public async Task<int> RemoveGameFromUser(Game game, User user)
    {
        return await _ctx.GameUsers
            .Where(gu => gu.GameId == game.Id && gu.UserId == user.Id)
            .ExecuteDeleteAsync();
    }
}

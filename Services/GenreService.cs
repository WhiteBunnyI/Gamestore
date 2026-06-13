using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections;


namespace Gamestore.Services;

public class GenreService
{
    private DbCtx _ctx;

    public GenreService(DbCtx db)
    {
        _ctx = db;
    }

    public async Task<int> Add(Genre genre)
    {
        genre.Name = genre.Name.Capitalize();

        return await _ctx.Genres
            .Upsert(genre)
            .On(g => g.Name)
            .NoUpdate()
            .RunAsync();
    }

    public async Task<List<string>?> GetAll() => await _ctx.Genres
        .Select(g => g.Name)
        .ToListAsync();

    public async Task<Genre?> Get(int id) => await _ctx.Genres.FindAsync(id);
    public async Task<Genre?> Get(string name) => await _ctx.Genres
        .Where(g => g.Name == name.Capitalize())
        .FirstOrDefaultAsync();
    public async Task<List<Genre>> Get(IEnumerable<int> ids) => await _ctx.Genres
        .Where(g => ids.Contains(g.Id))
        .ToListAsync();

    public async Task<int> Delete(int id) => await _ctx.Genres
        .Where(g => g.Id == id)
        .ExecuteDeleteAsync();

}

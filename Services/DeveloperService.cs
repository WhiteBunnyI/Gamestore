using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Collections;

namespace Gamestore.Services;

public class DeveloperService
{
    private DbCtx _ctx;

    public DeveloperService(DbCtx db)
    {
        _ctx = db;
    }

    public async Task<int> Add(Developer developer)
    {
        developer.Name = developer.Name.Capitalize();

        return await _ctx.Developers
            .Upsert(developer)
            .On(d => d.Name)
            .NoUpdate()
            .RunAsync();    
    }

    public async Task<Developer?> Get(int id) => await _ctx.Developers.FindAsync(id);
    public async Task<List<Developer>> Get(IEnumerable<int> ids)
    {
        return await _ctx.Developers
            .Where(d => ids.Contains(d.Id))
            .ToListAsync();
    }
    public async Task<Developer?> Get(string name) => await _ctx.Developers
        .Where(d => d.Name == name.Capitalize())
        .FirstOrDefaultAsync();

    public async Task<int> Delete(int id) => await _ctx.Developers
        .Where(d => d.Id == id)
        .ExecuteDeleteAsync();
}

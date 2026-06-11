using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Services;

public class CountryService
{
    DbCtx _ctx;

    public CountryService(DbCtx db)
    {
        _ctx = db;
    }


    public async Task<int> Add(Country country)
    {
        country.Name = country.Name.Capitalize();

        return await _ctx.Countries
            .Upsert(country)
            .On(c => c.Name)
            .NoUpdate()
            .RunAsync();
    }

    public async Task<Country?> Get(int id) => await _ctx.Countries.FindAsync(id);
    public async Task<Country?> Get(string name) => await _ctx.Countries
        .Where(c => c.Name == name.Capitalize())
        .FirstOrDefaultAsync();

    public async Task<int> Delete(int id) => await _ctx.Countries
        .Where(c => c.Id == id)
        .ExecuteDeleteAsync();
}

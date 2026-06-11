using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace Gamestore.Services;

public class PublisherService
{
    DbCtx _ctx;

    public PublisherService(DbCtx db)
    {
        _ctx = db;
    }

    public async Task<int> Add(Publisher publisher)
    {
        publisher.Name = publisher.Name.Capitalize();

        return await _ctx.Publishers
            .Upsert(publisher)
            .On(p => p.Name)
            .NoUpdate()
            .RunAsync();
    }

    public async Task<Publisher?> Get(int id) => await _ctx.Publishers.FindAsync(id);

    public async Task<Publisher?> Get(string name) => await _ctx.Publishers
            .Where(p => p.Name == name)
            .FirstOrDefaultAsync();

    public async Task<int> Delete(int id) => await _ctx.Publishers
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();
}

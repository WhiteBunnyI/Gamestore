using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Controllers
{
    [Route("api/publisher")]
    public class PublisherController : AppControllerBase
    {
        public PublisherController(DbCtx db, ILogger<PublisherController> logger) : base(db, logger)
        {
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(Publisher), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetPublisher(int? id, string? name)
        {
            Publisher? pub = null;

            if (id != null)
                pub ??= await _ctx.Publishers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            name?.Capitalize();
            if (name != null)
                pub ??= await _ctx.Publishers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name);

            if (pub == null)
                return Results.BadRequest();

            return Results.Ok(pub);
        }

        [HttpPost("add")]
        public async Task<IResult> AddPublisher(string pubName, string countryName)
        {
            pubName.Capitalize();
            countryName.Capitalize();

            if (await _ctx.Countries.FirstOrDefaultAsync(c => c.Name == countryName) is not Country country)
                return Results.BadRequest($"Страна {countryName} не найдена!");

            int added = await _ctx.Publishers
                .Upsert(new Publisher { Name = pubName, CountryId = country.Id })
                .On(p => p.Name)
                .RunAsync();

            if(added == 0)
                return Results.BadRequest($"Издатель {pubName} уже существует!");

            return Results.Ok($"Страна {countryName} была добавлена!");
        }
    }
}

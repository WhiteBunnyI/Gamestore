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

        [HttpPost("add")]
        public async Task<IResult> AddPublisher(string pubName, string countryName)
        {
            pubName = pubName.Capitalize();
            countryName = countryName.Capitalize();

            if (await _ctx.Countries.FirstOrDefaultAsync(c => c.Name == countryName) is not Country country)
                return Results.BadRequest($"Страна {countryName} не найдена!");

            int added = await _ctx.Publishers
                .Upsert(new Publisher { Name = pubName, CountryId = country.Id })
                .On(p => p.Name)
                .NoUpdate()
                .RunAsync();

            if (added == 0)
                return Results.BadRequest($"Издатель {pubName} уже существует!");

            return Results.Ok($"издатель {pubName} был добавлен!");
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(Publisher), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetPublisher(int? id, string? name)
        {
            Publisher? pub = null;

            if (id != null)
                pub ??= await _ctx.Publishers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            name = name?.Capitalize();
            if (name != null)
                pub ??= await _ctx.Publishers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name);

            if (pub == null)
                return Results.BadRequest();

            return Results.Ok(pub);
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> DeletePublisher(int id)
        {
            var check = await _ctx.Publishers.Where(p => p.Id == id).ExecuteDeleteAsync();
            if(check == 0)
                return Results.BadRequest();

            return Results.Ok();
        }
    }
}

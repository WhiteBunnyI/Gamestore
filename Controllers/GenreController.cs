using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Controllers
{
    [Route("api/genres")]
    public class GenreController : AppControllerBase
    {
        public GenreController(DbCtx db, ILogger<GenreController> logger) : base(db, logger) { }

        [HttpGet("get")]
        [ProducesResponseType(typeof(Genre), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetGenre(int? id, string? name)
        {
            Genre? genre = null;

            if (id != null)
                genre ??= await _ctx.Genres.FindAsync(id);

            name?.Capitalize();
            if (name != null)
                genre ??= await _ctx.Genres.FirstOrDefaultAsync(g => g.Name == name);

            if(genre == null)
                return Results.BadRequest();

            return Results.Ok(genre);
        }

        [HttpGet("get-all")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public async Task<IResult> GetAllGenres()
        {
            return Results.Ok(await _ctx.Genres.Select(g => g.Name).ToArrayAsync());
        }

        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddGenre(string name)
        {
            name.Capitalize();
            var check = await _ctx.Genres.FirstOrDefaultAsync(g => g.Name.Equals(name));

            int added = await _ctx.Genres
                .Upsert(new Genre { Name = name })
                .On(g => g.Name)
                .RunAsync();

            if(added == 0)
                return Results.Conflict($"Жанр {name} уже существует!");

            return Results.Ok();
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> RemoveGenre(string name)
        {
            name.Capitalize();
            var check = await _ctx.Genres.FirstOrDefaultAsync(g => g.Name.Equals(name));
            if (check == null) return Results.BadRequest($"Жанра {name} не существует!");

            await _ctx.Genres.Where(g => g.Name.Equals(name)).ExecuteDeleteAsync();

            return Results.Ok();
        }
    }
}
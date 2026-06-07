using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers
{
    [Route("api/genres")]
    public class GenreController : AppControllerBase
    {
        protected override string Entity => "Жанр";

        public GenreController(DbCtx db, ILogger<GenreController> logger) : base(db, logger) { }

        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddGenre([FromBody] Genre.GenreDto dto)
        {
            string name = dto.Name.Capitalize();
            var check = await _ctx.Genres.FirstOrDefaultAsync(g => g.Name.Equals(name));

            int added = await _ctx.Genres
                .Upsert(new Genre { Name = name })
                .On(g => g.Name)
                .NoUpdate()
                .RunAsync();

            if (added == 0)
                return Results.Conflict(CONFLICT_AUTO_MESSAGE);

            return Results.Ok();
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(Genre), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetGenre(int? id, string? name)
        {
            Genre? genre = null;

            if (id != null)
                genre ??= await _ctx.Genres.FindAsync(id);

            name = name?.Capitalize();
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

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> RemoveGenre(int id)
        {
            int deleted = 0;
            try
            {
                deleted = await _ctx.Genres
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
            }
            catch (DbException ex)
            when (ex is Npgsql.PostgresException pgEx && pgEx.SqlState.Equals(Npgsql.PostgresErrorCodes.ForeignKeyViolation))
            {
                return Results.BadRequest(FOREIGN_KEY_VIOLATION_MESSAGE);
            }

            if (deleted == 0) 
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok();
        }
    }
}
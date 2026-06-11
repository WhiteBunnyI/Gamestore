using Gamestore.Extensions;
using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers
{
    [Route("api/genres")]
    public class GenreController : AppControllerBase
    {
        protected override string Entity => "Жанр";

        private GenreService _genreService;

        public GenreController(DbCtx db, ILogger<GenreController> logger, GenreService genreService) : base(db, logger) { _genreService = genreService; }

        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddGenre([FromBody] Genre.GenreDto dto)
        {
            int added = await _genreService.Add(new Genre { Name = dto.Name });

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
                genre ??= await _genreService.Get(id.Value);

            if (name != null)
                genre ??= await _genreService.Get(name.Capitalize());

            if (genre == null)
                return Results.BadRequest();

            return Results.Ok(genre);
        }

        [HttpGet("get-all")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public async Task<IResult> GetAllGenres()
        {
            var lst = await _genreService.GetAll();

            return Results.Ok(lst);
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> RemoveGenre(int id)
        {
            int deleted = 0;
            try
            {
                deleted = await _genreService.Delete(id);
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
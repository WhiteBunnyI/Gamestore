using Gamestore.Extensions;
using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers
{
    [Route("api/publishers")]
    public class PublisherController : AppControllerBase
    {
        protected override string Entity => "Издатель";

        private CountryService _countryService;
        private PublisherService _publisherService;

        public PublisherController(DbCtx db, ILogger<PublisherController> logger, 
            CountryService countryService, PublisherService publisherService) : base(db, logger)
        {
            _countryService = countryService;
            _publisherService = publisherService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddPublisher([FromBody] Publisher.PublisherDto dto)
        {
            dto.PublisherName = dto.PublisherName.Capitalize();
            dto.CountryName = dto.CountryName.Capitalize();

            if (await _countryService.Get(dto.CountryName) is not Country country)
                return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE($"Страна {dto.CountryName}"));

            int added = await _publisherService.Add(new Publisher { Name = dto.PublisherName, CountryId = country.Id});

            if (added == 0)
                return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

            return Results.Ok(SUCCESS_ADDED_AUTO_MESSAGE);
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(Publisher), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetPublisher(int? id, string? name)
        {
            Publisher? pub = null;

            if (id != null)
                pub ??= await _publisherService.Get(id.Value);

            name = name?.Capitalize();
            if (name != null)
                pub ??= await _publisherService.Get(name);

            if (pub == null)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok(pub);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> DeletePublisher(int id)
        {
            int deleted = 0;
            try
            {
                deleted = await _publisherService.Delete(id);
            }
            catch (DbException ex)
            when (ex is Npgsql.PostgresException pgEx && pgEx.SqlState.Equals(Npgsql.PostgresErrorCodes.ForeignKeyViolation))
            {
                return Results.BadRequest(FOREIGN_KEY_VIOLATION_REFERENCE_MESSAGE);
            }

            if (deleted == 0)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok();
        }
    }
}

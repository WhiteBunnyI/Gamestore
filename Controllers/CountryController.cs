using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace Gamestore.Controllers;

[Route("api/country")]
public class CountryController : AppControllerBase
{
    protected override string Entity => "Страна";

    private CountryService _countryService;
    public CountryController(DbCtx db, ILogger<CountryController> logger, CountryService countryService) : base(db, logger)
    {
        _countryService = countryService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddCountry([FromBody] Country.CountryDto dto)
    {
        int added = await _countryService.Add(new Country { Name = dto.Name });

        if (added == 0)
            return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

        return Results.Ok(SUCCESS_ADDED_AUTO_MESSAGE);
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Country), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetCountry(int? id, string? name)
    {
        Country? country = null;

        if (id != null)
            country ??= await _countryService.Get(id.Value);
        if (name != null)
            country ??= await _countryService.Get(name);

        if (country == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok(country);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteCountry(int id)
    {
        int deleted = 0;
        try
        {
            deleted = await _countryService.Delete(id);
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

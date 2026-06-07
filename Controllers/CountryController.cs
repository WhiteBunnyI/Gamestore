using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers;

[Route("api/country")]
public class CountryController : AppControllerBase
{
    protected override string Entity => "Страна";

    public CountryController(DbCtx db, ILogger<CountryController> logger) : base(db, logger)
    {
    }

    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddCountry([FromBody] Country.CountryDto dto)
    {
        int added = await _ctx.Countries
            .Upsert(new Country { Name = dto.Name.Capitalize() })
            .On(c => c.Name)
            .NoUpdate()
            .RunAsync();

        if (added == 0)
            return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

        return Results.Ok();
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Country), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetCountry(int? id, string? name)
    {
        Country? country = null;

        if (id != null)
            country ??= await _ctx.Countries.FindAsync(id);
        if (name != null)
            country ??= await _ctx.Countries.Where(c => c.Name == name.Capitalize()).FirstOrDefaultAsync();

        if (country == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok(country);
    }

    [HttpDelete("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteCountry(int id)
    {
        int deleted = 0;
        try
        {
            deleted = await _ctx.Countries
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync();
        }
        catch (DbException ex)
        when(ex is Npgsql.PostgresException pgEx && pgEx.SqlState.Equals(Npgsql.PostgresErrorCodes.ForeignKeyViolation))
        {
            return Results.BadRequest(FOREIGN_KEY_VIOLATION_MESSAGE);
        }


        if (deleted == 0)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok();
    }
}

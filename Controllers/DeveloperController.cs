using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers;

[Route("api/developers")]
public class DeveloperController : AppControllerBase
{
    protected override string Entity => "Разработчик";

    public DeveloperController(DbCtx db, ILogger<DeveloperController> logger) : base(db, logger)
    {
    }

    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddDeveloper([FromBody] Developer.DeveloperDto dto)
    {
        if (await _ctx.Countries.FirstOrDefaultAsync(c => c.Name.Equals(dto.CountryName.Capitalize())) is not Country country)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE("Страна"));

        int added = await _ctx.Developers
            .Upsert(new Developer { Name = dto.DeveloperName, CountryId = country.Id })
            .On(d => d.Name)
            .NoUpdate()
            .RunAsync();

        if (added == 0)
            return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

        return Results.Ok();
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Developer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetDeveloper(int? id, string? name)
    {
        Developer? dev = null;
        if (id != null)
            dev ??= await _ctx.Developers.FindAsync(id);
        if(name != null)
            dev ??= await _ctx.Developers.FirstOrDefaultAsync(d => d.Name.Equals(name));

        if (dev == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok(dev);
    }

    [HttpDelete("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteDeveloper(int id)
    {
        int deleted;
        try
        {
            deleted = await _ctx.Developers
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

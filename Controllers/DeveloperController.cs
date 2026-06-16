using Gamestore.Extensions;
using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers;

[Route("api/developers")]
public class DeveloperController : AppControllerBase
{
    protected override string Entity => "Разработчик";

    private CountryService _countryService;
    private DeveloperService _developerService;

    public DeveloperController(DbCtx db, ILogger<DeveloperController> logger,
        CountryService countryService, DeveloperService developerService) : base(db, logger)
    {
        _countryService = countryService;
        _developerService = developerService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddDeveloper([FromBody] Developer.DeveloperDto dto)
    {
        if (await _countryService.Get(dto.CountryName) is not Country country)
            return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE("Страна"));

        int added = await _developerService.Add(new Developer { Name = dto.DeveloperName, CountryId = country.Id});

        if (added == 0)
            return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

        return Results.Ok(SUCCESS_ADDED_AUTO_MESSAGE);
    }

    [HttpGet("get")]
    [ProducesResponseType(typeof(Developer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetDeveloper(int? id, string? name)
    {
        Developer? dev = null;

        if (id != null)
            dev ??= await _developerService.Get(id.Value);
        if(name != null)
            dev ??= await _developerService.Get(name);

        if (dev == null)
            return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

        return Results.Ok(dev);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteDeveloper(int id)
    {
        int deleted;
        try
        {
            deleted = await _developerService.Delete(id);
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

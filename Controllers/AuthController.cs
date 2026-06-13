using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;

namespace Gamestore.Controllers;

[Route("api/auth")]
public class AuthController : AppControllerBase
{
    private IConfiguration _config;
    private AuthService _authService;
    public AuthController(DbCtx db, ILogger<AuthController> logger, IConfiguration configuration, AuthService authService) : base(db, logger)
    {
        _config = configuration;
        _authService = authService;
    }

    protected override string Entity => "Authentication";

    [HttpGet("refresh")]
    public IResult Refresh(string refreshString)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Trying to refresh with {refstr} with {ip}", refreshString, remoteIpAddress);

        if (string.IsNullOrEmpty(remoteIpAddress))
            return Results.BadRequest();

        var accessString = _authService.Refresh(refreshString, remoteIpAddress);
        if (string.IsNullOrEmpty(accessString))
            return Results.BadRequest();

        return Results.Ok(accessString);
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Login(string login, string password)
    {
        User user = new() { Id = -1, Login = login };
        //Пока только проверка логина
        if (login != AuthService.AdminLogin)
        {
            User? _user = await _ctx.Users.Where(u => u.Login == login).FirstOrDefaultAsync();
            if (_user == null)
                return Results.Unauthorized();
            user = _user;
        }

        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (remoteIpAddress == null)
            return Results.BadRequest();
        var (access, refresh) = _authService.Login(user, password, remoteIpAddress);            

        return Results.Ok(new { AccessToken = access, RefreshToken = refresh });
    }

    [Authorize]
    [HttpPut("logout")]
    public IResult Logout()
    {
        string jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
        long exp = long.Parse(User.FindFirstValue("exp")!);

        _authService.AddToBlacklist(jti, exp);

        return Results.Ok();
    }
}

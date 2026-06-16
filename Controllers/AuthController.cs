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
    const string _refreshCookieName = "refresh_token";
    const string _accessCookieName = "access_token";

    CookieOptions _refreshOptions = new CookieOptions()
    {
        HttpOnly = true, // Запрещает доступ к куки через JavaScript (защита от XSS)
        Secure = true,   // Отправлять только по HTTPS (обязательно для продакшена!)
        SameSite = SameSiteMode.Lax, // кросс-доменные запросы с навигацией
        Expires = DateTimeOffset.UtcNow.AddDays(30) // Срок жизни Refresh Token
    };

    CookieOptions _accessOptions = new CookieOptions()
    {
        HttpOnly = true, // Запрещает доступ к куки через JavaScript (защита от XSS)
        Secure = true,   // Отправлять только по HTTPS (обязательно для продакшена!)
        SameSite = SameSiteMode.Lax, // кросс-доменные запросы с навигацией
        Expires = DateTimeOffset.UtcNow.AddMinutes(10) // Срок жизни Token
    };

    private IConfiguration _config;
    private AuthService _authService;
    private UserService _userService;
    public AuthController(DbCtx db, ILogger<AuthController> logger, IConfiguration configuration, AuthService authService, UserService userService) : base(db, logger)
    {
        _config = configuration;
        _authService = authService;
        _userService = userService;
    }

    protected override string Entity => "Authentication";

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Register([FromBody] User.UserDto dto)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(remoteIpAddress))
            return Results.BadRequest("Unknown ip address");

        if (AuthService.AdminLogin.Equals(dto.Login))
            return Results.BadRequest("Нельзя регистрировать пользователя с таким именем");

        User user = new User() { Login = dto.Login };
        int added = await _userService.Add(user);

        if (added == 0)
            return Results.BadRequest(CONFLICT_EXACT_MESSAGE("Пользователь"));

        user = (await _userService.Get(dto.Login))!;

        var (access, refresh) = _authService.Login(user, dto.Password, remoteIpAddress);

        Response.Cookies.Append(_accessCookieName, access, _accessOptions);
        Response.Cookies.Append(_refreshCookieName, refresh, _refreshOptions);

        return Results.Ok(access);
    }

    [HttpGet("refresh")]
    public IResult Refresh()
    {
        string? refreshString = Request.Cookies[_refreshCookieName];

        if (string.IsNullOrEmpty(refreshString))
            return Results.BadRequest("Вы не были авторизированы!");

        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Trying to refresh with {refstr} with {ip}", refreshString, remoteIpAddress);

        if (string.IsNullOrEmpty(remoteIpAddress))
            return Results.BadRequest();

        var accessString = _authService.Refresh(refreshString, remoteIpAddress);
        if (string.IsNullOrEmpty(accessString))
            return Results.BadRequest();

        Response.Cookies.Append(_accessCookieName, accessString, _accessOptions);

        return Results.Ok(accessString);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Login([FromBody] User.UserDto dto)
    {
        User user = new() { Id = -1, Login = dto.Login };
        //Пока только проверка логина
        if (dto.Login != AuthService.AdminLogin)
        {
            User? _user = await _ctx.Users.Where(u => u.Login == dto.Login).FirstOrDefaultAsync();
            if (_user == null)
                return Results.BadRequest(NOT_FOUND_EXACT_MESSAGE("Пользователь"));
            user = _user;
        }

        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(remoteIpAddress))
            return Results.BadRequest("Unknown ip address");
        var (access, refresh) = _authService.Login(user, dto.Password, remoteIpAddress);

        Response.Cookies.Append(_refreshCookieName, refresh, _refreshOptions);
        Response.Cookies.Append(_accessCookieName, access, _accessOptions);

        return Results.Ok(access);
    }

    [HttpGet("status")]
    public IResult Status()
    {
        string? refreshCookie = Request.Cookies[_refreshCookieName];
        if (string.IsNullOrEmpty(refreshCookie))
            return Results.Ok(new { IsAuth = false });

        if(!_authService.IsRefreshTokenRegistered(refreshCookie))
            return Results.Ok(new { IsAuth = false });

        User user = _authService.GetUserByRefreshToken(refreshCookie)!;

        return Results.Ok(new { IsAuth = true, IsAdmin = AuthService.AdminLogin.Equals(user.Login), user.Login });
    }

    [Authorize]
    [HttpPost("logout")]
    public IResult Logout()
    {
        string jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
        long exp = long.Parse(User.FindFirstValue("exp")!);

        _authService.AddToBlacklist(jti, exp);

        Response.Cookies.Delete(_refreshCookieName);
        Response.Cookies.Delete(_accessCookieName);

        return Results.Ok();
    }
}

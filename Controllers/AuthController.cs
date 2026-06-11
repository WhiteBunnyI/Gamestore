using Gamestore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Gamestore.Controllers;

[Route("api/auth")]
public class AuthController : AppControllerBase
{
    private IConfiguration _config;
    public AuthController(DbCtx db, ILogger<AuthController> logger, IConfiguration configuration) : base(db, logger)
    {
        _config = configuration;
    }

    protected override string Entity => "Authentication";


    [HttpGet("login")]
    public async Task<IResult> Login(string login)
    {
        //Проверка логина и пароля
        //Пока только проверка логина
        if (await _ctx.Users.Where(u => u.Login == login).FirstOrDefaultAsync() is not User user)
            return Results.Unauthorized();

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, "User"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AuthSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["AuthSettings:Issuer"],
            audience: _config["AuthSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds
            );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(tokenString);
    }

    [Authorize]
    [HttpPut("logout")]
    public async Task<IResult> Logout()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        throw new NotImplementedException();
    }
}

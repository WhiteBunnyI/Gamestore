using Gamestore.Models;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Gamestore.Services;

public class AuthService
{
    public const string AdminLogin = "Admin";

    private ConcurrentBag<(string jti, long exp)> _jtiBlacklist = new();
    private ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();


    private ILogger<AuthService> _logger;
    private IConfiguration _config;
    public AuthService(ILogger<AuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration;
    }

    public (string access_token, string refresh_token) Login(User user, string password, string ipAddress)
    {
        //Пока без проверки пароля

        string tokenString = GenerateAccessToken(user);
        RefreshToken refreshToken = GenerateRefreshToken(user, ipAddress);

        var refreshString = Guid.NewGuid().ToString();

        _refreshTokens.AddOrUpdate(refreshString, refreshToken, (s, r) => refreshToken);

        return (tokenString, refreshString);
    }

    private string GenerateAccessToken(User user)
    {
        string jti = Guid.NewGuid().ToString();

        List<Claim> claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, "User"),
        };

        if (user.Login == AdminLogin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

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

        return tokenString;
    }

    private RefreshToken GenerateRefreshToken(User user, string ipAddress) => new()
    {
        Uid = Guid.NewGuid().ToString(),
        Expired = DateTimeOffset.UtcNow.AddDays(30),
        User = user,
        IpAddress = ipAddress
    };

    public string Refresh(string refreshString, string ipAddress)
    {
        string newTokenString = string.Empty;
        if (_refreshTokens.TryGetValue(refreshString, out var refreshToken))
        {
            if (refreshToken == null)
                return newTokenString;

            if (refreshToken.IpAddress.Equals(ipAddress))
            {
                User user = refreshToken.User;

                newTokenString = GenerateAccessToken(user);
                refreshToken = GenerateRefreshToken(user, ipAddress);

                _refreshTokens.AddOrUpdate(refreshString, refreshToken, (s, r) => refreshToken);

                _logger.LogInformation("{Login} updated his token (Ip: {ip})", user.Login, ipAddress);
            }
        }

        return newTokenString;
    }

    public void Logout(ClaimsPrincipal principal, string ipAddress)
    {
        string jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
        long exp = long.Parse(principal.FindFirstValue("exp")!);

        AddToBlacklist(jti, exp);
        _refreshTokens.TryRemove(jti, out _);
    }

    public void AddToBlacklist(string jti, long expUnix)
    {
        _jtiBlacklist.Add((jti, expUnix));
        _logger.LogInformation("Add to blacklist Jti: {jti}, {exp}. Now in the bag {count}.", jti, expUnix, _jtiBlacklist.Count);
    }


    public bool IsBlacklisted(string jti) => _jtiBlacklist.Where(c => c.jti.Equals(jti)).Any();

    public void CheckDateTime()
    {
        List<(string jti, long exp)> stillBlacklisted = new(_jtiBlacklist.Count * 2);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        //Check blacklist
        while (!_jtiBlacklist.IsEmpty)
        {
            if (_jtiBlacklist.TryTake(out var result))
            {
                if (result.exp > now)
                    stillBlacklisted.Add(result);
            }
        }

        foreach (var i in stillBlacklisted)
            _jtiBlacklist.Add(i);

        //Check refresh tokens
        foreach (var i in _refreshTokens)
        {
            var token = i.Value;
            if (token.Expired.ToUnixTimeSeconds() < now)
            {
                _refreshTokens.TryRemove(i);
            }
        }

        //_logger.LogInformation("Bag updated! Now in the bag {count}", _jtiBlacklist.Count);
    }

}


using Gamestore.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gamestore.Middlewares
{
    public class AuthenticationBlacklistMiddleware : IMiddleware
    {
        private readonly ILogger _logger;
        private readonly AuthService _authService;
        public AuthenticationBlacklistMiddleware(ILogger<AuthenticationBlacklistMiddleware> logger, AuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (!string.IsNullOrEmpty(jti))
                {
                    if (_authService.IsBlacklisted(jti))
                    {
                        _logger.LogInformation("In the black list");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Токен недействителен");
                        return;
                    }
                }
            }
            

            await next(context);
        }
    }
}

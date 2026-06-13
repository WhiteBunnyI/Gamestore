using Gamestore.Middlewares;
using Gamestore.Services;

namespace Gamestore.Extensions
{
    public static class Extensions
    {
        public static string Capitalize(this string text)
        {
            text = text.Trim();
            return char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
        }

        public static IApplicationBuilder UseAuthBlacklist(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AuthenticationBlacklistMiddleware>();
            return builder;
        }

        public static IServiceCollection AddGamestoreServices(this IServiceCollection services)
        {
            services.AddSingleton<AuthService>();
            services.AddScoped<CountryService>();
            services.AddScoped<DeveloperService>();
            services.AddScoped<GenreService>();
            services.AddScoped<PublisherService>();
            services.AddScoped<UserService>();
            services.AddScoped<GameService>();

            services.AddSingleton<AuthenticationBlacklistMiddleware>();
            services.AddHostedService<PeriodicService>();

            return services;
        }
    }
}

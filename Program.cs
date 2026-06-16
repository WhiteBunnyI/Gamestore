using Gamestore.Extensions;
using Gamestore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace Gamestore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            //Add postgresql

            var init = builder.Configuration["DbSettings:init"];
            var password = builder.Configuration["DbSettings:password"];
            string connectionString = $"{init};Password={password}";

            builder.Services.AddDbContextPool<DbCtx>(options => options.UseNpgsql(connectionString));
            builder.Services.AddGamestoreServices();
            builder.Services.AddHealthChecks().AddDbContextCheck<DbCtx>();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Введите ваш JWT токен."
                });

                c.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            var secretKey = builder.Configuration["AuthSettings:SecretKey"];
            if (secretKey == null || secretKey.Length == 0)
                throw new ApplicationException("Не найден секретный ключ или он пустой!");

            var jwtIssuer = builder.Configuration["AuthSettings:Issuer"];
            var jwtAudience = builder.Configuration["AuthSettings:Audience"];

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // Сюда бэкенд выведет точную причину (например, Expired или Invalid Signature)
                            Console.WriteLine("Ошибка авторизации: " + context.Exception.Message);
                            return Task.CompletedTask;
                        }
                    };
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.MapHealthChecks("/health");
            }

            // 1. Настраиваем обработку заголовков от ngrok
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthBlacklist();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            app.MapSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}

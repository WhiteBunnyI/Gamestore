using Gamestore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using System.Diagnostics;
using System.Xml.Linq;

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
            builder.Services.AddHealthChecks().AddDbContextCheck<DbCtx>();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.MapHealthChecks("/health");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            app.MapSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}

using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Controllers
{
    [Route("api/users")]
    public class UsersController : AppControllerBase
    {
        public UsersController(DbCtx db, ILogger<UsersController> logger) : base(db, logger) { }

        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        public async Task<IResult> AddUser(string login)
        {
            var check = await _ctx.Users.FirstOrDefaultAsync(u => u.Login == login);

            int added = await _ctx.Users
                .Upsert(new User() { Login = login })
                .On(u => u.Login)
                .NoUpdate()
                .RunAsync();

            if (added == 0)
                return Results.Conflict($"Пользователь {login} уже существует!");

            return Results.Ok($"Пользователь {login} был добавлен!");
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetUser(int? id, string? login)
        {
            User? user = null;

            if (id != null)
                user ??= await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (login != null)
                user ??= await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
                return Results.BadRequest();

            return Results.Ok(user);
        }

        [HttpPut("deposit")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> DepositBalance(string login, float value)
        {
            if (value <= 0) return Results.BadRequest("Нельзя пополнить сумму <= 0");

            var check = await _ctx.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (check == null) return Results.BadRequest($"Не найден пользователь с логином {login}");

            check.Wallet += value;
            await _ctx.SaveChangesAsync();

            return Results.Ok();
        }

        [HttpDelete("delete")]
        public async void DeleteUser(string login)
        {
            throw new NotImplementedException();
        }
    }
}

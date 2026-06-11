using Gamestore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;

namespace Gamestore.Controllers
{
    [Route("api/users")]
    public class UsersController : AppControllerBase
    {
        protected override string Entity => "Пользователь";

        public UsersController(DbCtx db, ILogger<UsersController> logger) : base(db, logger) {  }

        [HttpPost("add")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddUser([FromBody] User.UserDto dto)
        {
            int added = await _ctx.Users
                .Upsert(new User() { Login = dto.Login })
                .On(u => u.Login)
                .NoUpdate()
                .RunAsync();

            if (added == 0)
                return Results.BadRequest(CONFLICT_AUTO_MESSAGE);

            return Results.Ok(SUCCESS_AUTO_MESSAGE);
        }

        [HttpGet("get")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> GetUser(int? id, string? login)
        {
            User? user = null;

            if (id != null)
                user ??= await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (login != null)
                user ??= await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok(user);
        }

        [HttpPut("deposit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> DepositBalance(string login, float value)
        {
            if (value <= 0) return Results.BadRequest("Сумма (value) должна быть неотрицательной");

            int changed = await _ctx.Users.Where(u => u.Login == login)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Wallet, u => u.Wallet + value));

            if (changed == 0)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok();
        }

        [Authorize]
        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> DeleteUser()
        {
            if (User.Identity == null)
                return Results.Unauthorized();

            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            int deleted = 0;
            try
            {
                deleted = await _ctx.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
            }
            catch (DbException ex)
            when (ex is Npgsql.PostgresException pgEx && pgEx.SqlState.Equals(Npgsql.PostgresErrorCodes.ForeignKeyViolation))
            {
                return Results.BadRequest(FOREIGN_KEY_VIOLATION_MESSAGE);
            }

            if (deleted == 0)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok();
        }
    }
}

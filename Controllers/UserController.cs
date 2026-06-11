using Gamestore.Models;
using Gamestore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using System.Security.Claims;

namespace Gamestore.Controllers
{
    [Route("api/users")]
    public class UsersController : AppControllerBase
    {
        protected override string Entity => "Пользователь";
        private UserService _userService;

        public UsersController(DbCtx db, ILogger<UsersController> logger, UserService userService) : base(db, logger) { _userService = userService; }

        [HttpPost("add")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> AddUser([FromBody] User.UserDto dto)
        {
            int added = await _userService.Add(new User() { Login = dto.Login });

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
                user ??= await _userService.Get(id.Value);
            if (login != null)
                user ??= await _userService.Get(login);

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

            int changed = await _userService.DepositWallet(login, value);

            if (changed == 0)
                return Results.BadRequest(NOT_FOUND_AUTO_MESSAGE);

            return Results.Ok();
        }

        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IResult> DeleteUser()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            int deleted = 0;
            try
            {
                deleted = await _userService.Delete(id);
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

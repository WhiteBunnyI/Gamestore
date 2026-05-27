using Gamestore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.Controllers
{
    [Route("api/")]
    [ApiController]
    public class DbApiController : ControllerBase
    {
        private readonly AppDb _db;

        public DbApiController(AppDb db)
        {
            _db = db;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet]
        [Route("users/{id}")]
        public async Task<IResult> GetUser(int id)
        {
            return await _db.Users.FindAsync(id) is User user ? Results.Ok(user) : Results.NotFound();
        }
    }
}

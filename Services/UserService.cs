using Gamestore.Models;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Services;

public class UserService
{
    DbCtx _ctx;

    public UserService(DbCtx db)
    {
        _ctx = db;
    }

    public async Task<int> Add(User user)
    {
        return await _ctx.Users
                .Upsert(user)
                .On(u => u.Login)
                .NoUpdate()
                .RunAsync();
    }

    public async Task<User?> Get(int id)
    {
        return await _ctx.Users.FindAsync(id);
    }

    public async Task<User?> Get(string login)
    {
        return await _ctx.Users.Where(u => u.Login == login).FirstOrDefaultAsync();
    }

    public async Task<int> ChangeWallet(string login, float amount)
    {
        return await _ctx.Users
                .Where(u => u.Login == login)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Wallet, u => u.Wallet + amount));
    }

    public async Task<int> Delete(int id)
    {
        return await _ctx.Users
            .Where(u => u.Id == id)
            .ExecuteDeleteAsync();
    }
}

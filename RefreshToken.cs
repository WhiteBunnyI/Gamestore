using Gamestore.Models;

namespace Gamestore;

public record class RefreshToken
{
    public required string Uid;
    public required string IpAddress;
    public required DateTimeOffset Expired;
    public required User User;
}

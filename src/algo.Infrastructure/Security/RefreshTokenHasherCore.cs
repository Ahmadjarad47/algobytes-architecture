using System.Security.Cryptography;
using System.Text;
using algo.Application.Abstractions;

namespace algo.Infrastructure.Security;

public sealed class RefreshTokenHasherCore : IRefreshTokenHasher
{
    public string HashRefreshToken(string rawRefreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
}

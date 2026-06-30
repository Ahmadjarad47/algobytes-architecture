using System.Security.Cryptography;
using System.Text;
using algo.Application.Configuration;
using Microsoft.Extensions.Options;

namespace algo.Infrastructure.Identity;

public sealed class OtpHasher(IOptions<OtpOptions> options)
{
    private readonly OtpOptions _options = options.Value;

    public string HashCode(string plainCode)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_options.Pepper));
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(plainCode)));
    }
}

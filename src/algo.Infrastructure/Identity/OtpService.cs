using System.Security.Cryptography;
using System.Text;
using algo.Application.Abstractions;
using algo.Application.Configuration;
using Microsoft.Extensions.Options;

namespace algo.Infrastructure.Identity;

public sealed class OtpService(IOptions<OtpOptions> options) : IOtpService
{
    private readonly OtpOptions _options = options.Value;

    public string GenerateNumericCode(int length)
    {
        if (length < 4 || length > 12)
            throw new ArgumentOutOfRangeException(nameof(length));

        var bytes = RandomNumberGenerator.GetBytes(length);
        return string.Create(length, bytes, (span, buf) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = (char)('0' + (buf[i] % 10));
        });
    }

    public string HashCode(string plainCode)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_options.Pepper));
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(plainCode)));
    }

    public bool VerifyCode(string plainCode, string codeHash)
    {
        try
        {
            var expected = Convert.FromHexString(HashCode(plainCode));
            var actual = Convert.FromHexString(codeHash);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

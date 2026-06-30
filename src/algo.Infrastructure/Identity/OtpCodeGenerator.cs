using System.Security.Cryptography;
using algo.Application.Abstractions;

namespace algo.Infrastructure.Identity;

public sealed class OtpCodeGenerator(OtpHasher hasher) : IOtpCodeGenerator
{
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

    public string HashCode(string plainCode) => hasher.HashCode(plainCode);
}

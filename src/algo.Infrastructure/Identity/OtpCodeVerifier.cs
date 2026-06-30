using System.Security.Cryptography;
using algo.Application.Abstractions;

namespace algo.Infrastructure.Identity;

public sealed class OtpCodeVerifier(OtpHasher hasher) : IOtpCodeVerifier
{
    public bool VerifyCode(string plainCode, string codeHash)
    {
        try
        {
            var expected = Convert.FromHexString(hasher.HashCode(plainCode));
            var actual = Convert.FromHexString(codeHash);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

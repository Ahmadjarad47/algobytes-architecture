using System.Security.Cryptography;
using algo.Application.Abstractions;
using algo.Application.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace algo.Infrastructure.Security;

public sealed class RefreshTokenFactory(
    IOptions<JwtOptions> options,
    IRefreshTokenHasher refreshTokenHasher) : IRefreshTokenFactory
{
    private readonly JwtOptions _options = options.Value;

    public (string rawRefreshToken, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt) CreateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        var raw = WebEncoders.Base64UrlEncode(buffer);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        return (raw, refreshTokenHasher.HashRefreshToken(raw), expiresAt);
    }
}

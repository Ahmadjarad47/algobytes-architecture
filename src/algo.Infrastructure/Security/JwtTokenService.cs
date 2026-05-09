using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using algo.Application.Abstractions;
using algo.Application.Configuration;
using algo.Domain.Identity.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace algo.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string accessToken, DateTimeOffset accessTokenExpiresAt) CreateAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roleNames,
        Guid sessionId)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("User email is required before issuing a token.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("display_name", user.DisplayName),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
        };

        foreach (var role in roleNames)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string rawRefreshToken, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt) CreateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        var raw = WebEncoders.Base64UrlEncode(buffer);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        return (raw, HashRefreshToken(raw), expiresAt);
    }

    public string HashRefreshToken(string rawRefreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
}

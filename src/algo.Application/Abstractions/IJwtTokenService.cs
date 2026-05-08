using algo.Domain.Identity.Entities;

namespace algo.Application.Abstractions;

public interface IJwtTokenService
{
    (string accessToken, DateTimeOffset accessTokenExpiresAt) CreateAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roleNames);

    (string rawRefreshToken, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string rawRefreshToken);
}

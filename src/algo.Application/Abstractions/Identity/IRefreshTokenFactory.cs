namespace algo.Application.Abstractions.Identity;

public interface IRefreshTokenFactory
{
    (string rawRefreshToken, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt) CreateRefreshToken();
}


namespace algo.Application.Abstractions;

public interface IRefreshTokenFactory
{
    (string rawRefreshToken, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt) CreateRefreshToken();
}

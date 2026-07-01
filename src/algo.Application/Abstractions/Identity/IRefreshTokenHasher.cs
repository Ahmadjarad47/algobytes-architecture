namespace algo.Application.Abstractions;

public interface IRefreshTokenHasher
{
    string HashRefreshToken(string rawRefreshToken);
}

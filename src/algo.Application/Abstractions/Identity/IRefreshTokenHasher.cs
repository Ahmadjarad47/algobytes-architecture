namespace algo.Application.Abstractions.Identity;

public interface IRefreshTokenHasher
{
    string HashRefreshToken(string rawRefreshToken);
}


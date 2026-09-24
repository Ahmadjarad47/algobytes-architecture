using algo.Domain.Identity.Entities;

namespace algo.Application.Abstractions.Identity;

public interface IAccessTokenFactory
{
    (string accessToken, DateTimeOffset accessTokenExpiresAt) CreateAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roleNames,
        Guid sessionId);
}


using algo.Application.Abstractions;
using algo.Application.Features.Auth.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Identity;

public static class AuthSessionIssuer
{
    public static async Task<AuthResponseDto> IssueAsync(
        ApplicationUser user,
        IReadOnlyList<string> roleNames,
        IJwtTokenService jwt,
        IApplicationDbContext db,
        ISessionContext? sessionContext,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var (rawRefresh, refreshHash, refreshExp) = jwt.CreateRefreshToken();
        var (accessToken, accessExp) = jwt.CreateAccessToken(user, roleNames, sessionId);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = sessionId,
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExp,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            IpAddress = sessionContext?.IpAddress,
            Location = sessionContext?.Location,
            Device = sessionContext?.Device,
            Browser = sessionContext?.Browser,
            OperatingSystem = sessionContext?.OperatingSystem,
            UserAgent = sessionContext?.UserAgent,
            IsSuspicious = sessionContext?.IsSuspicious ?? false,
            IsTrustedDevice = !(sessionContext?.IsSuspicious ?? false),
        });

        await db.SaveChangesAsync(cancellationToken);

        var userDto = await BuildUserDtoAsync(user, roleNames, db, cancellationToken);
        var tokens = new TokenDto(
            accessToken,
            accessExp,
            new RefreshTokenDto(rawRefresh, refreshExp));

        return new AuthResponseDto(userDto, tokens);
    }

    public static async Task<UserDto> BuildUserDtoAsync(
        ApplicationUser user,
        IReadOnlyList<string> roleNames,
        IApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var permissionRows = await db.AccessPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.SubjectType == AccessPolicySubjectType.Role &&
                roleNames.Contains(policy.SubjectKey) &&
                policy.IsEnabled &&
                policy.DeletedAt == null &&
                policy.Effect == AccessPolicyEffect.Allow)
            .Select(policy => new { policy.Resource, policy.Action })
            .ToListAsync(cancellationToken);

        var permissions = permissionRows
            .Select(row => $"{row.Resource}:{row.Action}")
            .Distinct()
            .OrderBy(permission => permission)
            .ToList();

        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roleNames.OrderBy(role => role).ToArray(),
            permissions);
    }
}

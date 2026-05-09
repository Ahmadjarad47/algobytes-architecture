using algo.Application.Abstractions;
using algo.Application.Features.Auth.Dtos;
using algo.Application.Identity;
using algo.Domain.Identity.Entities;
using DomainRefreshToken = algo.Domain.Identity.Entities.RefreshToken;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwt,
    IApplicationDbContext db,
    ISessionContext sessionContext) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = jwt.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

        if (stored?.User is null || !stored.User.EmailConfirmed)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("authentication", "Invalid or expired refresh token. Please login again."),
            });
        }

        var (rawRefresh, newHash, refreshExp) = jwt.CreateRefreshToken();

        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = newHash;

        var sessionId = Guid.NewGuid();

        db.RefreshTokens.Add(new DomainRefreshToken
        {
            Id = sessionId,
            UserId = stored.UserId,
            TokenHash = newHash,
            ExpiresAt = refreshExp,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            IpAddress = sessionContext.IpAddress ?? stored.IpAddress,
            Location = sessionContext.Location ?? stored.Location,
            Device = sessionContext.Device,
            Browser = sessionContext.Browser,
            OperatingSystem = sessionContext.OperatingSystem,
            UserAgent = sessionContext.UserAgent ?? stored.UserAgent,
            IsSuspicious = sessionContext.IsSuspicious,
            IsTrustedDevice = !sessionContext.IsSuspicious,
        });

        var roles = (await userManager.GetRolesAsync(stored.User)).ToArray();
        var (accessToken, accessExp) = jwt.CreateAccessToken(stored.User, roles, sessionId);
        await db.SaveChangesAsync(cancellationToken);

        var userDto = await AuthSessionIssuer.BuildUserDtoAsync(stored.User, roles, db, cancellationToken);
        var tokens = new TokenDto(
            accessToken,
            accessExp,
            new RefreshTokenDto(rawRefresh, refreshExp));

        return new AuthResponseDto(userDto, tokens);
    }
}

using algo.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    IJwtTokenService jwt,
    IApplicationDbContext db) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unit.Value;
        }

        var hash = jwt.HashRefreshToken(request.RefreshToken);
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, cancellationToken);

        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

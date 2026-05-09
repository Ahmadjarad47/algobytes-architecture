using algo.Application.Abstractions;
using algo.Application.Features.Auth.Dtos;
using algo.Application.Identity;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwt,
    IApplicationDbContext db,
    ISessionContext sessionContext) : IRequestHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(string.Empty, "Invalid email or password."),
            });
        }

        if (!user.EmailConfirmed)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(LoginCommand.Email), "Email is not verified. Complete OTP activation first."),
            });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return await AuthSessionIssuer.IssueAsync(user, roles, jwt, db, sessionContext, cancellationToken);
    }
}

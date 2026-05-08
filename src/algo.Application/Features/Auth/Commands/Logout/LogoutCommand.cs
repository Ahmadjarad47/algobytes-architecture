using MediatR;

namespace algo.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : IRequest<Unit>;

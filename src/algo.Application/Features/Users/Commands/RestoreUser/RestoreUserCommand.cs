using MediatR;

namespace algo.Application.Features.Users.Commands.RestoreUser;

public sealed record RestoreUserCommand(string UserId) : IRequest<bool>;

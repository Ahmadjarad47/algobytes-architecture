using MediatR;

namespace algo.Application.Features.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(string UserId) : IRequest<Unit>;

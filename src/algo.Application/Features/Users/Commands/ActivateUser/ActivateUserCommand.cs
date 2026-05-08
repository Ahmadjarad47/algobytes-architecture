using MediatR;

namespace algo.Application.Features.Users.Commands.ActivateUser;

public sealed record ActivateUserCommand(string UserId) : IRequest<Unit>;

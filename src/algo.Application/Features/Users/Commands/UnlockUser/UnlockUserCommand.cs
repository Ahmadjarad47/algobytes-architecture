using MediatR;

namespace algo.Application.Features.Users.Commands.UnlockUser;

public sealed record UnlockUserCommand(string UserId) : IRequest<Unit>;

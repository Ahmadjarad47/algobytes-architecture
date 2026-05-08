using MediatR;

namespace algo.Application.Features.Users.Commands.LockUser;

public sealed record LockUserCommand(string UserId, DateTimeOffset LockoutEnd) : IRequest<Unit>;

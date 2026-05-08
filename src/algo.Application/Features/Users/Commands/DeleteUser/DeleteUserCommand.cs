using MediatR;

namespace algo.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(string UserId) : IRequest<bool>;

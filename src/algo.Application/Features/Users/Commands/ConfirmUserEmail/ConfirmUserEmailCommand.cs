using MediatR;

namespace algo.Application.Features.Users.Commands.ConfirmUserEmail;

public sealed record ConfirmUserEmailCommand(string UserId) : IRequest<Unit>;

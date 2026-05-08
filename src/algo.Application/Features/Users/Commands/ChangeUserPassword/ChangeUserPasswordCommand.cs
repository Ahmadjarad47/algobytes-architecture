using MediatR;

namespace algo.Application.Features.Users.Commands.ChangeUserPassword;

public sealed record ChangeUserPasswordCommand(
    string UserId,
    string NewPassword,
    string ConfirmPassword) : IRequest<Unit>;

using MediatR;

namespace algo.Application.Features.Users.Commands.SetUserTotpPolicy;

public sealed record SetUserTotpPolicyCommand(string UserId, bool IsRequired) : IRequest<bool>;

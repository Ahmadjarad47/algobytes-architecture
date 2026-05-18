using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.RestoreAccessPolicy;

public sealed record RestoreAccessPolicyCommand(Guid Id) : IRequest<bool>;

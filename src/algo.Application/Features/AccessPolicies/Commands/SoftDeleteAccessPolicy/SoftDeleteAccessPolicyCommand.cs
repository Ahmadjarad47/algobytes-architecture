using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.SoftDeleteAccessPolicy;

public sealed record SoftDeleteAccessPolicyCommand(Guid Id) : IRequest<bool>;

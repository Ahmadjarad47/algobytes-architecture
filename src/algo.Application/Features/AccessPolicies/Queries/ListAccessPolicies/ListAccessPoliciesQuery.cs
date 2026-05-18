using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Queries.ListAccessPolicies;

public sealed record ListAccessPoliciesQuery(
    bool IncludeTrashed = false,
    bool OnlyTrashed = false) : IRequest<IReadOnlyList<AccessPolicyAdminDto>>;

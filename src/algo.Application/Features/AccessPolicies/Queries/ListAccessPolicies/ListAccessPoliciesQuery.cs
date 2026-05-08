using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Queries.ListAccessPolicies;

public sealed record ListAccessPoliciesQuery : IRequest<IReadOnlyList<AccessPolicyAdminDto>>;

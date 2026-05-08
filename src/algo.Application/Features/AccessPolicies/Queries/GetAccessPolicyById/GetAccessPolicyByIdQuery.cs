using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyById;

public sealed record GetAccessPolicyByIdQuery(Guid Id) : IRequest<AccessPolicyAdminDto?>;

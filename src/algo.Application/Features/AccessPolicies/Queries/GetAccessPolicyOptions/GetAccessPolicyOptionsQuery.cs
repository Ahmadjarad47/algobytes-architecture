using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyOptions;

public sealed record GetAccessPolicyOptionsQuery : IRequest<AccessPolicyOptionsDto>;

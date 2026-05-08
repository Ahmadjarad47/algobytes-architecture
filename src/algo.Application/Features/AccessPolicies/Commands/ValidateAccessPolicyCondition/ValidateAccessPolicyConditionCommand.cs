using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.ValidateAccessPolicyCondition;

public sealed record ValidateAccessPolicyConditionCommand(string Resource, string ConditionJson)
    : IRequest<ValidateAccessPolicyConditionResultDto>;

using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.ValidateAccessPolicyCondition;

public sealed class ValidateAccessPolicyConditionCommandHandler(
    IAccessPolicyConditionParser conditionParser,
    IAccessPolicyMetadataProvider metadataProvider) : IRequestHandler<ValidateAccessPolicyConditionCommand,
    ValidateAccessPolicyConditionResultDto>
{
    public Task<ValidateAccessPolicyConditionResultDto> Handle(
        ValidateAccessPolicyConditionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!metadataProvider.TryGetMetadata(request.Resource, out _))
            {
                return Task.FromResult(new ValidateAccessPolicyConditionResultDto(
                    false,
                    $"Unknown resource '{request.Resource}'."));
            }

            var ast = conditionParser.Parse(request.ConditionJson);
            conditionParser.Validate(request.Resource, ast, metadataProvider);
            return Task.FromResult(new ValidateAccessPolicyConditionResultDto(true, null));
        }
        catch (AccessPolicyConditionParseException ex)
        {
            return Task.FromResult(new ValidateAccessPolicyConditionResultDto(false, ex.Message));
        }
        catch (AccessPolicyConditionValidationException ex)
        {
            return Task.FromResult(new ValidateAccessPolicyConditionResultDto(false, ex.Message));
        }
    }
}

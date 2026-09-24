using algo.Application.Common.AccessPolicy;

namespace algo.Application.Abstractions.Services;

public interface IAccessPolicyConditionParser
{
    AccessPolicyConditionAst Parse(string? conditionJson);

    void Validate(string resource, AccessPolicyConditionAst ast, IAccessPolicyMetadataLookup metadataLookup);
}


namespace algo.Application.Common.AccessPolicy;

public sealed class AccessPolicyEntityMetadata
{
    public required Type EntityType { get; init; }

    public required IReadOnlyDictionary<string, AccessPolicyFieldMetadata> Fields { get; init; }
}

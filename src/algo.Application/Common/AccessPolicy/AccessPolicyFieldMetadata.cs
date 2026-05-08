namespace algo.Application.Common.AccessPolicy;

public sealed class AccessPolicyFieldMetadata
{
    public required string PropertyName { get; init; }

    public required Type ClrType { get; init; }
}

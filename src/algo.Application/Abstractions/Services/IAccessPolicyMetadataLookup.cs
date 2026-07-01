using algo.Application.Common.AccessPolicy;

namespace algo.Application.Abstractions.Services;

public interface IAccessPolicyMetadataLookup
{
    bool TryGetMetadata(string resource, out AccessPolicyEntityMetadata? metadata);
}


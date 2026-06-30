using algo.Application.Common.AccessPolicy;

namespace algo.Application.Abstractions;

public interface IAccessPolicyMetadataLookup
{
    bool TryGetMetadata(string resource, out AccessPolicyEntityMetadata? metadata);
}

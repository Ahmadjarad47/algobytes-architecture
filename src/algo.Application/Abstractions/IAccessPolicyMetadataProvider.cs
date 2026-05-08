using algo.Application.Common.AccessPolicy;

namespace algo.Application.Abstractions;

public interface IAccessPolicyMetadataProvider
{
    bool TryGetMetadata(string resource, out AccessPolicyEntityMetadata? metadata);

    IReadOnlyCollection<string> GetRegisteredResources();
}

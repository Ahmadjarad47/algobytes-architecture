namespace algo.Application.Abstractions.Services;

public interface IAccessPolicyResourceCatalog
{
    IReadOnlyCollection<string> GetRegisteredResources();
}


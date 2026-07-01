namespace algo.Application.Abstractions;

public interface IAccessPolicyResourceCatalog
{
    IReadOnlyCollection<string> GetRegisteredResources();
}

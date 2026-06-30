using algo.Domain.CustomFields;

namespace algo.Application.Abstractions;

public interface ICustomFieldIndexSynchronizer
{
    Task SyncIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
}

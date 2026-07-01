using algo.Domain.CustomFields;

namespace algo.Application.Abstractions.Persistence;

public interface ICustomFieldIndexSynchronizer
{
    Task SyncIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
}


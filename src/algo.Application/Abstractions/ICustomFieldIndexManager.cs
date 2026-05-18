using algo.Domain.CustomFields;

namespace algo.Application.Abstractions;

public interface ICustomFieldIndexManager
{
    Task SyncIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);

    Task DropIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
}

using algo.Domain.CustomFields;

namespace algo.Application.Abstractions.Persistence;

public interface ICustomFieldIndexDropper
{
    Task DropIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
}


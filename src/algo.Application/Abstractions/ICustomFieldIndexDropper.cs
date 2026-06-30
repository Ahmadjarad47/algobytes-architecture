using algo.Domain.CustomFields;

namespace algo.Application.Abstractions;

public interface ICustomFieldIndexDropper
{
    Task DropIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
}

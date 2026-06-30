using algo.Application.Abstractions;
using algo.Domain.CustomFields;
using algo.Persistence.Context;

namespace algo.Persistence.Services;

internal sealed class CustomFieldIndexDropper(ApplicationDbContext db) : ICustomFieldIndexDropper
{
    public Task DropIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken) =>
        CustomFieldIndexSql.DropIndexesAsync(db, definition, cancellationToken);
}

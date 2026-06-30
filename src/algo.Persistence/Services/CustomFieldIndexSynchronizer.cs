using algo.Application.Abstractions;
using algo.Domain.CustomFields;
using algo.Persistence.Context;

namespace algo.Persistence.Services;

internal sealed class CustomFieldIndexSynchronizer(ApplicationDbContext db) : ICustomFieldIndexSynchronizer
{
    public Task SyncIndexesAsync(CustomFieldDefinition definition, CancellationToken cancellationToken) =>
        CustomFieldIndexSql.SyncIndexesAsync(db, definition, cancellationToken);
}

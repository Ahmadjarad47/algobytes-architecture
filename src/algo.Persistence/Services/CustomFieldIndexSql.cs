using algo.Domain.CustomFields;
using algo.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace algo.Persistence.Services;

internal static class CustomFieldIndexSql
{
    public static async Task DropIndexesAsync(
        ApplicationDbContext db,
        CustomFieldDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!IsPostgres(db))
        {
            return;
        }

        var mapping = ResolveTable(definition.Entity);
        if (mapping is null)
        {
            return;
        }

        var resolved = mapping.Value;

        await db.Database.ExecuteSqlRawAsync(
            $"""DROP INDEX IF EXISTS "{BuildIndexName(resolved.TableName, definition.Key)}";""",
            cancellationToken);
    }

    public static async Task SyncIndexesAsync(
        ApplicationDbContext db,
        CustomFieldDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!IsPostgres(db))
        {
            return;
        }

        var mapping = ResolveTable(definition.Entity);
        if (mapping is null)
        {
            return;
        }

        var resolved = mapping.Value;

        await DropIndexesAsync(db, definition, cancellationToken);

        if (definition.Searchable || definition.Filterable || definition.Sortable)
        {
            var sql = $"""
                CREATE INDEX "{BuildIndexName(resolved.TableName, definition.Key)}"
                ON "{resolved.TableName}" ({BuildExpression(definition)});
                """;
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private static bool IsPostgres(ApplicationDbContext db) =>
        string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);

    private static (string TableName, string ColumnName)? ResolveTable(string entity) =>
        entity switch
        {
            "users" => ("AspNetUsers", "CustomFields"),
            "roles" => ("AspNetRoles", "CustomFields"),
            "accessPolicies" => ("access_policies", "CustomFields"),
            _ => null
        };

    private static string BuildIndexName(string tableName, string key)
    {
        var sanitized = key.ToLowerInvariant();
        return $"ix_{tableName.ToLowerInvariant()}_cf_{sanitized}";
    }

    private static string BuildExpression(CustomFieldDefinition definition)
    {
        var key = definition.Key.Replace("\"", string.Empty, StringComparison.Ordinal);
        return definition.Type switch
        {
            CustomFieldType.Number => $"""((("CustomFields" ->> '{key}')::numeric))""",
            CustomFieldType.Boolean => $"""((("CustomFields" ->> '{key}')::boolean))""",
            _ => $"""(("CustomFields" ->> '{key}'))"""
        };
    }
}

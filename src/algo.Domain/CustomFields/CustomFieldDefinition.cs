using System.Text.Json;

namespace algo.Domain.CustomFields;

public sealed class CustomFieldDefinition
{
    public Guid Id { get; set; }

    public string Entity { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public CustomFieldType Type { get; set; }

    public bool Required { get; set; }

    public bool Searchable { get; set; }

    public bool Filterable { get; set; }

    public bool Sortable { get; set; }

    public bool VisibleInTable { get; set; } = true;

    public bool VisibleInForm { get; set; } = true;

    public bool VisibleInDetails { get; set; } = true;

    public JsonDocument? OptionsJson { get; set; }

    public JsonDocument? DefaultValueJson { get; set; }

    public JsonDocument? ValidationJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

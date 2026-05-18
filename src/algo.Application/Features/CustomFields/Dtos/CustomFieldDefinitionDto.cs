using System.Text.Json;
using algo.Domain.CustomFields;

namespace algo.Application.Features.CustomFields.Dtos;

public sealed record CustomFieldDefinitionDto(
    Guid Id,
    string Entity,
    string Key,
    string Label,
    CustomFieldType Type,
    bool Required,
    bool Searchable,
    bool Filterable,
    bool Sortable,
    bool VisibleInTable,
    bool VisibleInForm,
    bool VisibleInDetails,
    JsonElement? Options,
    JsonElement? DefaultValue,
    JsonElement? Validation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

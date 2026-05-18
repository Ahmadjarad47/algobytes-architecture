using algo.Application.Common.CustomFields;
using algo.Application.Features.CustomFields.Dtos;
using algo.Domain.CustomFields;

namespace algo.Application.Features.CustomFields;

internal static class CustomFieldDefinitionMapping
{
    public static CustomFieldDefinitionDto ToDto(this CustomFieldDefinition definition) =>
        new(
            definition.Id,
            definition.Entity,
            definition.Key,
            definition.Label,
            definition.Type,
            definition.Required,
            definition.Searchable,
            definition.Filterable,
            definition.Sortable,
            definition.VisibleInTable,
            definition.VisibleInForm,
            definition.VisibleInDetails,
            JsonDocumentHelpers.CloneToElement(definition.OptionsJson),
            JsonDocumentHelpers.CloneToElement(definition.DefaultValueJson),
            JsonDocumentHelpers.CloneToElement(definition.ValidationJson),
            definition.CreatedAt,
            definition.UpdatedAt);
}

using System.Text.Json;
using algo.Domain.CustomFields;

namespace algo.Application.Features.CustomFields.Commands.UpdateCustomFieldDefinition;

public sealed record UpdateCustomFieldDefinitionRequest(
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
    JsonElement? Validation)
{
    public UpdateCustomFieldDefinitionCommand ToCommand(Guid id) => new(
        id,
        Label,
        Type,
        Required,
        Searchable,
        Filterable,
        Sortable,
        VisibleInTable,
        VisibleInForm,
        VisibleInDetails,
        Options,
        DefaultValue,
        Validation);
}

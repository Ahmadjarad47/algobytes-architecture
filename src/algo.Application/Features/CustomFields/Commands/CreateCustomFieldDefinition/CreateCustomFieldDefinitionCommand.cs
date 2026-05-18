using System.Text.Json;
using algo.Application.Features.CustomFields.Dtos;
using algo.Domain.CustomFields;
using MediatR;

namespace algo.Application.Features.CustomFields.Commands.CreateCustomFieldDefinition;

public sealed record CreateCustomFieldDefinitionCommand(
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
    JsonElement? Validation) : IRequest<CustomFieldDefinitionDto>;

using algo.Application.Features.CustomFields.Dtos;
using MediatR;

namespace algo.Application.Features.CustomFields.Queries.ListCustomFieldDefinitions;

public sealed record ListCustomFieldDefinitionsQuery(string Entity) : IRequest<IReadOnlyList<CustomFieldDefinitionDto>>;

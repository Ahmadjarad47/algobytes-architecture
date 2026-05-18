using MediatR;

namespace algo.Application.Features.CustomFields.Commands.DeleteCustomFieldDefinition;

public sealed record DeleteCustomFieldDefinitionCommand(Guid Id) : IRequest<bool>;

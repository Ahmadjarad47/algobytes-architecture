using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.CustomFields.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.CustomFields.Commands.UpdateCustomFieldDefinition;

public sealed class UpdateCustomFieldDefinitionCommandHandler(
    IApplicationDbContext db,
    ICustomFieldIndexSynchronizer indexSynchronizer)
    : IRequestHandler<UpdateCustomFieldDefinitionCommand, CustomFieldDefinitionDto?>
{
    public async Task<CustomFieldDefinitionDto?> Handle(UpdateCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await db.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        var label = request.Label.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Label), "Label is required.")
            });
        }

        definition.Label = label;
        definition.Type = request.Type;
        definition.Required = request.Required;
        definition.Searchable = request.Searchable;
        definition.Filterable = request.Filterable;
        definition.Sortable = request.Sortable;
        definition.VisibleInTable = request.VisibleInTable;
        definition.VisibleInForm = request.VisibleInForm;
        definition.VisibleInDetails = request.VisibleInDetails;
        definition.OptionsJson = JsonDocumentHelpers.CloneToDocument(request.Options);
        definition.DefaultValueJson = JsonDocumentHelpers.CloneToDocument(request.DefaultValue);
        definition.ValidationJson = JsonDocumentHelpers.CloneToDocument(request.Validation);
        definition.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await indexSynchronizer.SyncIndexesAsync(definition, cancellationToken);

        return definition.ToDto();
    }
}

using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.CustomFields.Dtos;
using algo.Domain.CustomFields;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.CustomFields.Commands.CreateCustomFieldDefinition;

public sealed class CreateCustomFieldDefinitionCommandHandler(
    IApplicationDbContext db,
    ICustomFieldIndexSynchronizer indexSynchronizer)
    : IRequestHandler<CreateCustomFieldDefinitionCommand, CustomFieldDefinitionDto>
{
    public async Task<CustomFieldDefinitionDto> Handle(CreateCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Entity.Trim();
        var key = request.Key.Trim();
        var label = request.Label.Trim();

        Validate(entity, key, label);

        if (await db.CustomFieldDefinitions.AnyAsync(d => d.Entity == entity && d.Key == key, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Key), $"Custom field '{key}' already exists for '{entity}'.")
            });
        }

        var utcNow = DateTimeOffset.UtcNow;
        var definition = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            Entity = entity,
            Key = key,
            Label = label,
            Type = request.Type,
            Required = request.Required,
            Searchable = request.Searchable,
            Filterable = request.Filterable,
            Sortable = request.Sortable,
            VisibleInTable = request.VisibleInTable,
            VisibleInForm = request.VisibleInForm,
            VisibleInDetails = request.VisibleInDetails,
            OptionsJson = JsonDocumentHelpers.CloneToDocument(request.Options),
            DefaultValueJson = JsonDocumentHelpers.CloneToDocument(request.DefaultValue),
            ValidationJson = JsonDocumentHelpers.CloneToDocument(request.Validation),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        db.CustomFieldDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        await indexSynchronizer.SyncIndexesAsync(definition, cancellationToken);

        return definition.ToDto();
    }

    private static void Validate(string entity, string key, string label)
    {
        if (!CustomFieldEntities.Supported.Contains(entity))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(entity), $"Unsupported custom field entity '{entity}'.")
            });
        }

        if (string.IsNullOrWhiteSpace(key) || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-zA-Z][a-zA-Z0-9_]*$"))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(key), "Key must start with a letter and contain only letters, numbers, and underscores.")
            });
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(label), "Label is required.")
            });
        }
    }
}

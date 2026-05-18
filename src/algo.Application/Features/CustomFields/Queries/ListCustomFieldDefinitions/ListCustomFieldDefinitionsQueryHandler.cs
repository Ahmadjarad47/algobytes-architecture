using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.CustomFields.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.CustomFields.Queries.ListCustomFieldDefinitions;

public sealed class ListCustomFieldDefinitionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>
{
    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> Handle(ListCustomFieldDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var entity = request.Entity.Trim();
        if (!CustomFieldEntities.Supported.Contains(entity))
        {
            return [];
        }

        var definitions = await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(definition => definition.Entity == entity)
            .OrderBy(definition => definition.Label)
            .ToListAsync(cancellationToken);

        return definitions.Select(definition => definition.ToDto()).ToList();
    }
}

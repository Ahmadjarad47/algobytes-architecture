using algo.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.CustomFields.Commands.DeleteCustomFieldDefinition;

public sealed class DeleteCustomFieldDefinitionCommandHandler(
    IApplicationDbContext db,
    ICustomFieldIndexManager indexManager)
    : IRequestHandler<DeleteCustomFieldDefinitionCommand, bool>
{
    public async Task<bool> Handle(DeleteCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await db.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (definition is null)
        {
            return false;
        }

        db.CustomFieldDefinitions.Remove(definition);
        await db.SaveChangesAsync(cancellationToken);
        await indexManager.DropIndexesAsync(definition, cancellationToken);
        return true;
    }
}

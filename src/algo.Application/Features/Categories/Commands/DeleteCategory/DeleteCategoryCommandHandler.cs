using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Delete,
            cancellationToken);

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return false;

        var hasProducts = await db.Products
            .AsNoTracking()
            .AnyAsync(p => p.CategoryId == request.Id, cancellationToken);

        if (hasProducts)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DeleteCategoryCommand.Id), "Category cannot be deleted while products are assigned."),
            });
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

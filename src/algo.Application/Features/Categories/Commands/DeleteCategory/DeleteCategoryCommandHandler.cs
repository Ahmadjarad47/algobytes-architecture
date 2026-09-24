using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Trash;
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.Id == request.Id && c.DeletedAt == null,
                cancellationToken);

        if (category is null)
            return false;

        var hasProducts = await db.Products
            .AsNoTracking()
            .AnyAsync(
                p => p.CategoryId == request.Id && p.DeletedAt == null && p.TrashedAt == null,
                cancellationToken);

        if (hasProducts)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DeleteCategoryCommand.Id), "Category cannot be deleted while products are assigned."),
            });
        }

        var utcNow = DateTimeOffset.UtcNow;
        category.TrashedAt = utcNow;
        category.TrashExpiresAt = utcNow.Add(TrashRetention.Duration);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

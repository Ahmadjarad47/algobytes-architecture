using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Categories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Queries.GetAllCategories;

public sealed class GetAllCategoriesQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = (request.IncludeTrashed || request.OnlyTrashed
                ? db.Categories.IgnoreQueryFilters()
                : db.Categories)
            .AsNoTracking();

        if (request.OnlyTrashed)
        {
            query = query.Where(category => category.TrashedAt != null && category.DeletedAt == null);
        }
        else if (!request.IncludeTrashed)
        {
            query = query.Where(category => category.TrashedAt == null && category.DeletedAt == null);
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.Products.Count,
                c.TrashedAt,
                c.TrashExpiresAt,
                c.DeletedAt))
            .ToListAsync(cancellationToken);
    }
}

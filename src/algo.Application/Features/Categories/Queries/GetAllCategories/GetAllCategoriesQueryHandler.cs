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

        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.Products.Count))
            .ToListAsync(cancellationToken);
    }
}

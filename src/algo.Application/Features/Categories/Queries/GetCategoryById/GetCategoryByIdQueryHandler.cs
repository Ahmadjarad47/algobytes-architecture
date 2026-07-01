using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Categories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDetailsDto?>
{
    public async Task<CategoryDetailsDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Read,
            cancellationToken);

        return await db.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == request.Id && c.DeletedAt == null)
            .Select(c => new CategoryDetailsDto(
                c.Id,
                c.Name,
                c.Description,
                c.Products.Count,
                c.TrashedAt,
                c.TrashExpiresAt,
                c.DeletedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

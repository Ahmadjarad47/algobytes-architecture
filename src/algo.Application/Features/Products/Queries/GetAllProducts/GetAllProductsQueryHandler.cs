using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Queries.GetAllProducts;

public sealed class GetAllProductsQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = (request.IncludeTrashed || request.OnlyTrashed
                ? db.Products.IgnoreQueryFilters()
                : db.Products)
            .AsNoTracking();

        if (request.OnlyTrashed)
        {
            query = query.Where(product => product.TrashedAt != null && product.DeletedAt == null);
        }
        else if (!request.IncludeTrashed)
        {
            query = query.Where(product => product.TrashedAt == null && product.DeletedAt == null);
        }

        return await query
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.CategoryId,
                p.Category.Name,
                p.PriceUsd,
                p.PriceSyp,
                p.DiscountedPriceUsd,
                p.DiscountedPriceSyp,
                p.ExternalGameId,
                p.Provider,
                p.ImageUrl,
                p.CreatedAt,
                p.UpdatedAt,
                p.TrashedAt,
                p.TrashExpiresAt,
                p.DeletedAt))
            .ToListAsync(cancellationToken);
    }
}

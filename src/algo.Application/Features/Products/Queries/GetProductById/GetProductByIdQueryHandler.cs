using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Read,
            cancellationToken);

        return await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Id == request.Id)
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
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

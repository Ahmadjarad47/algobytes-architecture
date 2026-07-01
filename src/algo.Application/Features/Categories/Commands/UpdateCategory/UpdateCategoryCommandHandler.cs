using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Categories.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<UpdateCategoryCommand, CategoryDetailsDto?>
{
    public async Task<CategoryDetailsDto?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Update,
            cancellationToken);

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return null;

        var name = request.Name.Trim();

        if (await db.Categories.AnyAsync(c => c.Id != request.Id && c.Name == name, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateCategoryCommand.Name), $"Category '{name}' already exists."),
            });
        }

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await db.SaveChangesAsync(cancellationToken);

        var productCount = await db.Products
            .AsNoTracking()
            .CountAsync(p => p.CategoryId == category.Id, cancellationToken);

        return new CategoryDetailsDto(category.Id, category.Name, category.Description, productCount);
    }
}

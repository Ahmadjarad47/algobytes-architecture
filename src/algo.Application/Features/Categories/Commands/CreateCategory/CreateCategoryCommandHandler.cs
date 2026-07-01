using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Categories.Dtos;
using algo.Domain.Catalog.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<CreateCategoryCommand, CategoryDetailsDto>
{
    public async Task<CategoryDetailsDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Create,
            cancellationToken);

        var name = request.Name.Trim();

        if (await db.Categories.AnyAsync(c => c.Name == name, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateCategoryCommand.Name), $"Category '{name}' already exists."),
            });
        }

        var category = new Category
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryDetailsDto(category.Id, category.Name, category.Description, 0);
    }
}

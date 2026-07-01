using algo.Application.Features.Categories.Dtos;
using MediatR;

namespace algo.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDetailsDto>;

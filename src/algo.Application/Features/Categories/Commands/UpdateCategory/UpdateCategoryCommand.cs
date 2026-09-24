using algo.Application.Features.Categories.Dtos;
using MediatR;

namespace algo.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(int Id, string Name, string? Description, string? ImageUrl) : IRequest<CategoryDetailsDto?>;

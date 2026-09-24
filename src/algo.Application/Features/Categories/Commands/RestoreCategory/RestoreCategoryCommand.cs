using MediatR;

namespace algo.Application.Features.Categories.Commands.RestoreCategory;

public sealed record RestoreCategoryCommand(int Id) : IRequest<bool>;

using algo.Application.Features.Storage.Dtos;
using MediatR;

namespace algo.Application.Features.Storage.Queries.GetStorageSettings;

public sealed record GetStorageSettingsQuery : IRequest<StorageSettingsDto>;

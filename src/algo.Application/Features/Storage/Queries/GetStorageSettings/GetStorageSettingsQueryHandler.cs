using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Storage.Dtos;
using algo.Application.Features.Storage.StorageConfigurationMapping;
using algo.Domain.Storage.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Storage.Queries.GetStorageSettings;

public sealed class GetStorageSettingsQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetStorageSettingsQuery, StorageSettingsDto>
{
    public async Task<StorageSettingsDto> Handle(GetStorageSettingsQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Settings,
            AccessPolicyActions.Read,
            cancellationToken);

        var configuration = await db.StorageConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == StorageConfiguration.SingletonId, cancellationToken);

        configuration ??= StorageConfigurationDefaults.Create();

        return StorageConfigurationMapper.ToDto(configuration);
    }
}

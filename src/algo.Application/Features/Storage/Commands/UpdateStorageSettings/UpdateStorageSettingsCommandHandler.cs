using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Storage.Dtos;
using algo.Application.Features.Storage.StorageConfigurationMapping;
using algo.Domain.Storage.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Storage.Commands.UpdateStorageSettings;

public sealed class UpdateStorageSettingsCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<UpdateStorageSettingsCommand, StorageSettingsDto>
{
    public async Task<StorageSettingsDto> Handle(UpdateStorageSettingsCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Settings,
            AccessPolicyActions.Update,
            cancellationToken);

        var configuration = await db.StorageConfigurations
            .FirstOrDefaultAsync(c => c.Id == StorageConfiguration.SingletonId, cancellationToken);

        if (configuration is null)
        {
            configuration = StorageConfigurationDefaults.Create();
            db.StorageConfigurations.Add(configuration);
        }

        if (string.IsNullOrWhiteSpace(configuration.SecretKey) && string.IsNullOrWhiteSpace(request.SecretKey))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateStorageSettingsCommand.SecretKey), "Secret key is required for the initial storage configuration."),
            });
        }

        configuration.EndpointUrl = request.EndpointUrl.Trim();
        configuration.AccessKey = request.AccessKey.Trim();
        if (!string.IsNullOrWhiteSpace(request.SecretKey))
            configuration.SecretKey = request.SecretKey.Trim();

        configuration.BucketName = request.BucketName.Trim();
        configuration.Region = request.Region.Trim();
        configuration.Folder = request.Folder.Trim().Trim('/');
        configuration.UsePathStyle = request.UsePathStyle;
        configuration.ScannerEnabled = request.ScannerEnabled;
        configuration.ScannerProvider = request.ScannerProvider.Trim();
        configuration.ScannerEndpointUrl = string.IsNullOrWhiteSpace(request.ScannerEndpointUrl)
            ? null
            : request.ScannerEndpointUrl.Trim();
        configuration.ScannerApiKey = string.IsNullOrWhiteSpace(request.ScannerApiKey)
            ? null
            : request.ScannerApiKey.Trim();
        configuration.QuarantineFolder = string.IsNullOrWhiteSpace(request.QuarantineFolder)
            ? null
            : request.QuarantineFolder.Trim();
        configuration.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return StorageConfigurationMapper.ToDto(configuration);
    }
}

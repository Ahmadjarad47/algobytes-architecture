using algo.Application.Abstractions.Persistence;
using algo.Application.Abstractions.Storage;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Storage.Dtos;
using algo.Domain.Storage.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Storage.Commands.UploadProductImage;

public sealed class UploadProductImageCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IObjectStorageService objectStorageService,
    IFileScannerService fileScannerService)
    : IRequestHandler<UploadProductImageCommand, UploadProductImageResultDto>
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    public async Task<UploadProductImageResultDto> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Create,
            cancellationToken);

        if (request.Length <= 0 || request.Length > MaxImageBytes)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UploadProductImageCommand.Length), "Image must be between 1 byte and 10 MB."),
            });
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UploadProductImageCommand.ContentType), "Only JPEG, PNG, WEBP, and GIF images are supported."),
            });
        }

        var configuration = await db.StorageConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == StorageConfiguration.SingletonId, cancellationToken);

        if (configuration is null
            || string.IsNullOrWhiteSpace(configuration.EndpointUrl)
            || string.IsNullOrWhiteSpace(configuration.AccessKey)
            || string.IsNullOrWhiteSpace(configuration.SecretKey)
            || string.IsNullOrWhiteSpace(configuration.BucketName))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("storage", "Amazon S3 storage is not configured. Save storage settings first."),
            });
        }

        await using var scanStream = new MemoryStream();
        await request.Content.CopyToAsync(scanStream, cancellationToken);
        scanStream.Position = 0;

        var scanResult = await fileScannerService.ScanAsync(
            new FileScanRequest(
                scanStream,
                request.FileName,
                configuration.ScannerEnabled,
                configuration.ScannerProvider,
                configuration.ScannerEndpointUrl,
                configuration.ScannerApiKey),
            cancellationToken);

        if (string.Equals(scanResult.Status, "infected", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("file", scanResult.Message),
            });
        }

        if (string.Equals(scanResult.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("file", scanResult.Message),
            });
        }

        scanStream.Position = 0;
        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ContentTypeToExtension(request.ContentType);

        var objectKey = BuildObjectKey(configuration.Folder, extension);
        var url = await objectStorageService.UploadAsync(
            new ObjectStorageUploadRequest(
                scanStream,
                objectKey,
                request.ContentType,
                configuration.EndpointUrl,
                configuration.AccessKey,
                configuration.SecretKey,
                configuration.BucketName,
                configuration.Region,
                configuration.UsePathStyle),
            cancellationToken);

        return new UploadProductImageResultDto(url);
    }

    private static string BuildObjectKey(string folder, string extension)
    {
        var prefix = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().Trim('/');
        return $"{prefix}/products/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    private static string ContentTypeToExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg",
        };
}

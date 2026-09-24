using algo.Application.Abstractions.Persistence;
using algo.Application.Abstractions.Storage;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Storage.Dtos;
using algo.Domain.Storage.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Storage.Commands.ScanFile;

public sealed class ScanFileCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IFileScannerService fileScannerService)
    : IRequestHandler<ScanFileCommand, FileScanResultDto>
{
    private const long MaxScanBytes = 25 * 1024 * 1024;

    public async Task<FileScanResultDto> Handle(ScanFileCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Settings,
            AccessPolicyActions.Update,
            cancellationToken);

        if (request.Length <= 0 || request.Length > MaxScanBytes)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ScanFileCommand.Length), "File must be between 1 byte and 25 MB."),
            });
        }

        var configuration = await db.StorageConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == StorageConfiguration.SingletonId, cancellationToken);

        configuration ??= new StorageConfiguration { Id = StorageConfiguration.SingletonId };

        var scanResult = await fileScannerService.ScanAsync(
            new FileScanRequest(
                request.Content,
                request.FileName,
                configuration.ScannerEnabled,
                configuration.ScannerProvider,
                configuration.ScannerEndpointUrl,
                configuration.ScannerApiKey),
            cancellationToken);

        return new FileScanResultDto(
            scanResult.FileName,
            scanResult.Size,
            scanResult.Status,
            scanResult.Engine,
            scanResult.Message,
            scanResult.ScannedAt);
    }
}

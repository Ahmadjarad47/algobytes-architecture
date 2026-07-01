using algo.Application.Features.Storage.Dtos;
using MediatR;

namespace algo.Application.Features.Storage.Commands.UpdateStorageSettings;

public sealed record UpdateStorageSettingsCommand(
    string EndpointUrl,
    string AccessKey,
    string SecretKey,
    string BucketName,
    string Region,
    string Folder,
    bool UsePathStyle,
    bool ScannerEnabled,
    string ScannerProvider,
    string? ScannerEndpointUrl,
    string? ScannerApiKey,
    string? QuarantineFolder) : IRequest<StorageSettingsDto>;

public sealed record UpdateStorageSettingsRequest(
    UpdateStorageSettingsStorageRequest Storage,
    UpdateStorageSettingsScannerRequest Scanner);

public sealed record UpdateStorageSettingsStorageRequest(
    string EndpointUrl,
    string AccessKey,
    string SecretKey,
    string BucketName,
    string Region,
    string Folder,
    bool UsePathStyle);

public sealed record UpdateStorageSettingsScannerRequest(
    bool Enabled,
    string Provider,
    string? EndpointUrl,
    string? ApiKey,
    string? QuarantineFolder);

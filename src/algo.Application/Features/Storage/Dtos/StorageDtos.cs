namespace algo.Application.Features.Storage.Dtos;

public sealed record S3StorageSettingsDto(
    string Provider,
    string EndpointUrl,
    string AccessKey,
    string? SecretKeyMasked,
    string BucketName,
    string Region,
    string Folder,
    bool UsePathStyle);

public sealed record FileScannerSettingsDto(
    bool Enabled,
    string Provider,
    string? EndpointUrl,
    string? ApiKey,
    string? QuarantineFolder);

public sealed record StorageSettingsDto(
    string Source,
    DateTimeOffset? UpdatedAt,
    S3StorageSettingsDto Storage,
    FileScannerSettingsDto Scanner);

public sealed record UploadProductImageResultDto(string Url);

public sealed record FileScanResultDto(
    string FileName,
    long Size,
    string Status,
    string Engine,
    string Message,
    DateTimeOffset ScannedAt);

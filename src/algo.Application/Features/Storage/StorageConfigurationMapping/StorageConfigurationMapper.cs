using algo.Application.Features.Storage.Dtos;
using algo.Domain.Storage.Entities;

namespace algo.Application.Features.Storage.StorageConfigurationMapping;

internal static class StorageConfigurationDefaults
{
    public static StorageConfiguration Create() => new()
    {
        Id = StorageConfiguration.SingletonId,
        EndpointUrl = "https://s3.amazonaws.com",
        AccessKey = string.Empty,
        SecretKey = string.Empty,
        BucketName = string.Empty,
        Region = "us-east-1",
        Folder = "uploads",
        UsePathStyle = false,
        ScannerEnabled = false,
        ScannerProvider = "clamav",
        UpdatedAt = DateTimeOffset.UtcNow,
    };
}

internal static class StorageConfigurationMapper
{
    public static StorageSettingsDto ToDto(StorageConfiguration configuration) =>
        new(
            "database",
            configuration.UpdatedAt == default ? null : configuration.UpdatedAt,
            new S3StorageSettingsDto(
                "s3",
                configuration.EndpointUrl,
                configuration.AccessKey,
                MaskSecret(configuration.SecretKey),
                configuration.BucketName,
                configuration.Region,
                configuration.Folder,
                configuration.UsePathStyle),
            new FileScannerSettingsDto(
                configuration.ScannerEnabled,
                configuration.ScannerProvider,
                configuration.ScannerEndpointUrl,
                configuration.ScannerApiKey,
                configuration.QuarantineFolder));

    public static string MaskSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        if (secret.Length <= 4)
            return new string('*', secret.Length);

        return $"{secret[..4]}{new string('*', Math.Min(secret.Length - 4, 12))}";
    }
}

namespace algo.Domain.Storage.Entities;

public class StorageConfiguration
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string EndpointUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";

    public string Folder { get; set; } = "uploads";

    public bool UsePathStyle { get; set; }

    public bool ScannerEnabled { get; set; }

    public string ScannerProvider { get; set; } = "clamav";

    public string? ScannerEndpointUrl { get; set; }

    public string? ScannerApiKey { get; set; }

    public string? QuarantineFolder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

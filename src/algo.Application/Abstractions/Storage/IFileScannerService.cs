namespace algo.Application.Abstractions.Storage;

public sealed record FileScanRequest(
    Stream Content,
    string FileName,
    bool ScannerEnabled,
    string ScannerProvider,
    string? ScannerEndpointUrl,
    string? ScannerApiKey);

public sealed record FileScanResponse(
    string FileName,
    long Size,
    string Status,
    string Engine,
    string Message,
    DateTimeOffset ScannedAt);

public interface IFileScannerService
{
    Task<FileScanResponse> ScanAsync(FileScanRequest request, CancellationToken cancellationToken = default);
}

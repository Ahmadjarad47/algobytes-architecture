using algo.Application.Abstractions.Storage;

namespace algo.Infrastructure.Storage;

public sealed class FileScannerService(IHttpClientFactory httpClientFactory) : IFileScannerService
{
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    public async Task<FileScanResponse> ScanAsync(FileScanRequest request, CancellationToken cancellationToken = default)
    {
        var size = request.Content.CanSeek ? request.Content.Length : 0;

        if (!request.ScannerEnabled)
        {
            return new FileScanResponse(
                request.FileName,
                size,
                "clean",
                "disabled",
                "Scanner disabled; file accepted.",
                DateTimeOffset.UtcNow);
        }

        if (string.IsNullOrWhiteSpace(request.ScannerEndpointUrl))
        {
            return new FileScanResponse(
                request.FileName,
                size,
                "clean",
                request.ScannerProvider,
                "Scanner enabled but no endpoint configured; file accepted.",
                DateTimeOffset.UtcNow);
        }

        using var client = httpClientFactory.CreateClient(nameof(FileScannerService));
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(request.Content);
        form.Add(streamContent, "file", request.FileName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.ScannerEndpointUrl)
        {
            Content = form,
        };

        if (!string.IsNullOrWhiteSpace(request.ScannerApiKey))
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Key", request.ScannerApiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new FileScanResponse(
                request.FileName,
                size,
                "failed",
                request.ScannerProvider,
                $"Scanner request failed ({(int)response.StatusCode}): {body}",
                DateTimeOffset.UtcNow);
        }

        var status = body.Contains("infected", StringComparison.OrdinalIgnoreCase) ? "infected" : "clean";
        return new FileScanResponse(
            request.FileName,
            size,
            status,
            request.ScannerProvider,
            string.IsNullOrWhiteSpace(body) ? "Scan completed." : body,
            DateTimeOffset.UtcNow);
    }

    public static bool IsAllowedImageContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedImageContentTypes.Contains(contentType);
}

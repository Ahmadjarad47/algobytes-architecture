using algo.Application.Abstractions.Storage;
using Amazon.S3;
using Amazon.S3.Model;

namespace algo.Infrastructure.Storage;

public sealed class S3ObjectStorageService : IObjectStorageService
{
    public async Task<string> UploadAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken = default)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = request.EndpointUrl.TrimEnd('/'),
            ForcePathStyle = request.UsePathStyle,
            AuthenticationRegion = request.Region,
        };

        using var client = new AmazonS3Client(request.AccessKey, request.SecretKey, config);

        var putRequest = new PutObjectRequest
        {
            BucketName = request.BucketName,
            Key = request.ObjectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false,
        };

        await client.PutObjectAsync(putRequest, cancellationToken);

        return BuildPublicUrl(request, request.ObjectKey);
    }

    private static string BuildPublicUrl(ObjectStorageUploadRequest request, string objectKey)
    {
        var endpoint = request.EndpointUrl.TrimEnd('/');
        var encodedKey = string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString));

        if (request.UsePathStyle)
            return $"{endpoint}/{request.BucketName}/{encodedKey}";

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            && !string.IsNullOrWhiteSpace(endpointUri.Host)
            && endpointUri.Host.StartsWith("s3.", StringComparison.OrdinalIgnoreCase))
        {
            return $"{endpointUri.Scheme}://{request.BucketName}.{endpointUri.Host}/{encodedKey}";
        }

        return $"{endpoint}/{request.BucketName}/{encodedKey}";
    }
}

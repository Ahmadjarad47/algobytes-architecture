using algo.Application.Abstractions.Storage;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net.Http;
using System.Net.Sockets;

namespace algo.Infrastructure.Storage;

public sealed class S3ObjectStorageService : IObjectStorageService
{
    private const string InternalStorageOrigin = "http://72.61.187.165:9000";
    private const string PublicStorageOrigin = "https://fileserver.aljawharaplus.com";

    public async Task<string> UploadAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken = default)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = request.EndpointUrl.TrimEnd('/'),
            ForcePathStyle = request.UsePathStyle,
            AuthenticationRegion = request.Region,
            Timeout = TimeSpan.FromSeconds(10),
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

        try
        {
            await client.PutObjectAsync(putRequest, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx &&
                                             socketEx.SocketErrorCode == SocketError.HostNotFound)
        {
            throw new ObjectStorageUnavailableException(
                "Storage endpoint host could not be resolved. Verify endpoint URL and DNS settings.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ObjectStorageUnavailableException(
                "Storage endpoint is unreachable. Verify endpoint URL, network access, and SSL configuration.",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ObjectStorageUnavailableException(
                "Storage upload timed out. Verify endpoint URL and network connectivity.",
                ex);
        }

        return BuildPublicUrl(request, request.ObjectKey);
    }

    private static string BuildPublicUrl(ObjectStorageUploadRequest request, string objectKey)
    {
        var endpoint = MapEndpointForPublicUrl(request.EndpointUrl.TrimEnd('/'));
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

    private static string MapEndpointForPublicUrl(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            !Uri.TryCreate(InternalStorageOrigin, UriKind.Absolute, out var internalUri) ||
            !Uri.TryCreate(PublicStorageOrigin, UriKind.Absolute, out var publicUri))
        {
            return endpoint;
        }

        var matchesInternalOrigin =
            string.Equals(endpointUri.Host, internalUri.Host, StringComparison.OrdinalIgnoreCase) &&
            endpointUri.Port == internalUri.Port;

        if (!matchesInternalOrigin)
        {
            return endpoint;
        }

        return $"{publicUri.Scheme}://{publicUri.Authority}";
    }
}

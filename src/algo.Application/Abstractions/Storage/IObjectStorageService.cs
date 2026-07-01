namespace algo.Application.Abstractions.Storage;

public sealed record ObjectStorageUploadRequest(
    Stream Content,
    string ObjectKey,
    string ContentType,
    string EndpointUrl,
    string AccessKey,
    string SecretKey,
    string BucketName,
    string Region,
    bool UsePathStyle);

public sealed class ObjectStorageUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IObjectStorageService
{
    Task<string> UploadAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken = default);
}

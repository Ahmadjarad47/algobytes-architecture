using algo.Application.Features.Storage.Dtos;
using MediatR;

namespace algo.Application.Features.Storage.Commands.UploadProductImage;

public sealed record UploadProductImageCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string FolderSegment,
    string AuthorizedResource,
    IReadOnlyCollection<string> AuthorizedActions) : IRequest<UploadProductImageResultDto>;

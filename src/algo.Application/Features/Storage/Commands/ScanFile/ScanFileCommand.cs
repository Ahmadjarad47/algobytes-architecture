using algo.Application.Features.Storage.Dtos;
using MediatR;

namespace algo.Application.Features.Storage.Commands.ScanFile;

public sealed record ScanFileCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IRequest<FileScanResultDto>;

using algo.Application.Features.Storage.Commands.ScanFile;
using algo.Application.Features.Storage.Commands.UpdateStorageSettings;
using algo.Application.Features.Storage.Commands.UploadProductImage;
using algo.Application.Features.Storage.Dtos;
using algo.Application.Features.Storage.Queries.GetStorageSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[Authorize]
[Route("api/v1/storage")]
public sealed class StorageController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [HttpGet("settings")]
    [ProducesResponseType(typeof(StorageSettingsDto), StatusCodes.Status200OK)]
    public Task<StorageSettingsDto> GetSettings(CancellationToken cancellationToken) =>
        mediator.Send(new GetStorageSettingsQuery(), cancellationToken);

    [HttpPut]
    [HttpPut("settings")]
    [ProducesResponseType(typeof(StorageSettingsDto), StatusCodes.Status200OK)]
    public Task<StorageSettingsDto> UpdateSettings(
        [FromBody] UpdateStorageSettingsRequest body,
        CancellationToken cancellationToken) =>
        mediator.Send(
            new UpdateStorageSettingsCommand(
                body.Storage.EndpointUrl,
                body.Storage.AccessKey,
                body.Storage.SecretKey,
                body.Storage.BucketName,
                body.Storage.Region,
                body.Storage.Folder,
                body.Storage.UsePathStyle,
                body.Scanner.Enabled,
                body.Scanner.Provider,
                body.Scanner.EndpointUrl,
                body.Scanner.ApiKey,
                body.Scanner.QuarantineFolder),
            cancellationToken);

    [HttpPost("scanner/scan")]
    [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(26_214_400)]
    public async Task<ActionResult<FileScanResultDto>> ScanFile(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is required.");

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new ScanFileCommand(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("upload/product-image")]
    [ProducesResponseType(typeof(UploadProductImageResultDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<UploadProductImageResultDto>> UploadProductImage(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Image file is required.");

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadProductImageCommand(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return Ok(result);
    }
}

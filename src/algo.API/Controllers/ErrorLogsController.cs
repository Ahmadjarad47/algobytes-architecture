using algo.Application.Common.Identity;
using algo.Application.Common.Pagination;
using algo.Application.Features.ErrorLogs.Dtos;
using algo.Application.Features.ErrorLogs.Queries.GetErrorLogById;
using algo.Application.Features.ErrorLogs.Queries.GetErrorLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/error-logs")]
public sealed class ErrorLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ErrorLogDto>), StatusCodes.Status200OK)]
    public Task<PaginatedResult<ErrorLogDto>> List(
        [FromQuery] GetErrorLogsQueryParameters parameters,
        CancellationToken cancellationToken) =>
        mediator.Send(parameters.ToQuery(), cancellationToken);

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ErrorLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ErrorLogDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetErrorLogByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}

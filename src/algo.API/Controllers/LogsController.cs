using algo.Application.Common.Identity;
using algo.Application.Common.Pagination;
using algo.Application.Features.Logs.Dtos;
using algo.Application.Features.Logs.Queries.GetLogById;
using algo.Application.Features.Logs.Queries.GetLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ApplicationLogDto>), StatusCodes.Status200OK)]
    public Task<PaginatedResult<ApplicationLogDto>> List(
        [FromQuery] GetLogsQueryParameters parameters,
        CancellationToken cancellationToken) =>
        mediator.Send(parameters.ToQuery(), cancellationToken);

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApplicationLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationLogDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLogByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}

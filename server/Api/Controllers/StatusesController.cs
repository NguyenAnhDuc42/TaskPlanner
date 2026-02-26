using Application.Features.StatusManagement.GetStatusList;
using Application.Features.StatusManagement.SyncStatuses;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatusesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatusesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatuses(
        [FromQuery] Guid layerId,
        [FromQuery] EntityLayerType layerType,
        CancellationToken cancellationToken)
    {
        var query = new GetStatusListQuery(layerId, layerType);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncStatuses([FromBody] SyncStatusesCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

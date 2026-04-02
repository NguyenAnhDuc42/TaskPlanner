using Application.Features.StatusManagement.SyncStatuses;
using Application.Features.StatusManagement.GetStatusList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkflowsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{workflowId:guid}/statuses")]
    public async Task<IActionResult> GetStatuses(Guid workflowId, CancellationToken cancellationToken)
    {
        var query = new GetStatusListQuery(workflowId);
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

using Application.Features.WorkflowFeatures;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatusesController(IHandler handler) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableStatuses(
        [FromQuery] Guid? spaceId,
        [FromQuery] Guid? folderId,
        CancellationToken ct)
    {
        var result = await handler.QueryAsync<GetAvailableStatusesQuery, List<StatusResponse>>(
            new GetAvailableStatusesQuery(spaceId, folderId), ct);
        return result.ToActionResult();
    }

    [HttpPut("workflow/{workflowId:guid}")]
    public async Task<IActionResult> UpdateWorkflowStatuses(
        Guid workflowId, 
        [FromBody] List<StatusUpdateDto> statuses, 
        CancellationToken ct)
    {
        var result = await handler.SendAsync(new UpdateWorkflowStatusesCommand(workflowId, statuses), ct);
        return result.ToActionResult();
    }
}

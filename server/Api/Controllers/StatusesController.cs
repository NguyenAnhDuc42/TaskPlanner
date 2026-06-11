using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class StatusesController(IHandler handler) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableStatuses(
        [FromQuery] Guid? spaceId,
        [FromQuery] Guid? folderId,
        CancellationToken cancellationToken)
    {
        var result = await handler.QueryAsync<GetAvailableStatusesQuery, List<StatusRecord>>(
            new GetAvailableStatusesQuery(spaceId, folderId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("workflow/{workflowId:guid}")]
    public async Task<IActionResult> UpdateWorkflowStatuses(
        Guid workflowId, 
        [FromBody] List<StatusUpdateValue> statuses, 
        CancellationToken cancellationToken)
    {
        var result = await handler.SendAsync(new UpdateWorkflowStatusesCommand(workflowId, statuses), cancellationToken);
        return result.ToActionResult();
    }
}



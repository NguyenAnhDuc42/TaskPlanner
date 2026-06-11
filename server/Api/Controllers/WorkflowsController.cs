using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class WorkflowsController(IHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceWorkflows([FromQuery] Guid? layerId, [FromQuery] string? layerType, CancellationToken cancellationToken)
    {
        var query = new GetWorkspaceWorkflowsQuery(layerId, layerType);
        var result = await handler.QueryAsync<GetWorkspaceWorkflowsQuery, List<WorkflowRecord>>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("set-layer")]
    public async Task<IActionResult> SetLayerWorkflow([FromBody] SetLayerWorkflowCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }
}



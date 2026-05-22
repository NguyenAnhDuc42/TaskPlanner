using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]
public class WorkflowsController(IHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceWorkflows([FromQuery] Guid? layerId, [FromQuery] string? layerType, CancellationToken ct)
    {
        var query = new GetWorkspaceWorkflowsQuery(layerId, layerType);
        var result = await handler.QueryAsync<GetWorkspaceWorkflowsQuery, List<WorkflowDto>>(query, ct);
        return result.ToActionResult();
    }

    [HttpPost("set-layer")]
    public async Task<IActionResult> SetLayerWorkflow([FromBody] SetLayerWorkflowCommand command, CancellationToken ct)
    {
        var result = await handler.SendAsync(command, ct);
        return result.ToActionResult();
    }
}



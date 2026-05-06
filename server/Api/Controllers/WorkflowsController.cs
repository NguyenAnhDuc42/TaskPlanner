using Application.Features.WorkflowFeatures;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WorkflowsController(IHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceWorkflows(CancellationToken ct)
    {
        var query = new GetWorkspaceWorkflowsQuery();
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

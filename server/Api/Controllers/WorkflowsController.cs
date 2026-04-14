using Application.Features.WorkflowFeatures.SetLayerWorkflow;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WorkflowsController(IHandler handler) : ControllerBase
{
    [HttpPost("set-layer")]
    public async Task<IActionResult> SetLayerWorkflow([FromBody] SetLayerWorkflowCommand command, CancellationToken ct)
    {
        var result = await handler.SendAsync(command, ct);
        return result.ToActionResult();
    }
}

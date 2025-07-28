using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.DashboardWorkspace.GetTasks;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpGet("{workspaceId}/dashboard/tasks")]
    public async Task<IActionResult> GetTasks([FromRoute] Guid workspaceId, CancellationToken cancellationToken)
    {
        var request = new GetTasksRequest(workspaceId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.SidebarWorkspaces;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpGet("{workspaceId}/sidebar")]
    public async Task<IActionResult> SidebarWorkspaces([FromRoute]Guid workspaceId ,CancellationToken cancellationToken)
    {
        var request = new SidebarWorkspacesRequest(workspaceId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.SidebarWorkspaces;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpGet("sidebar")]
    public async Task<IActionResult> SidebarWorkspaces(CancellationToken cancellationToken)
    {
        var request = new SidebarWorkspacesRequest();
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

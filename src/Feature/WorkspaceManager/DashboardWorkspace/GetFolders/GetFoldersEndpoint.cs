using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.DashboardWorkspace.GetFolders;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpGet("{workspaceId}/dashboard/folders")]
    public async Task<IActionResult> GetFolders([FromRoute] Guid workspaceId)
    {
        var result = await _mediator.Send(new GetFoldersRequest(workspaceId));
        return result.ToApiResult();
    }
}

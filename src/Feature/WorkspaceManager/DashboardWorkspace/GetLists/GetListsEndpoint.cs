using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.DashboardWorkspace.GetLists;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpGet("{workspaceId}/dashboard/lists")]
    public async Task<IActionResult> GetLists([FromRoute] Guid workspaceId)
    {
        var result = await _mediator.Send(new GetListsRequest(workspaceId));
        return result.ToApiResult();
    }
}

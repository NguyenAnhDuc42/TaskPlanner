using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.Workspace.ShowMembers;
using src.Helper.Results;

namespace src.Feature.Workspace;

public partial class WorkspaceController 
{
    [HttpGet("{workspaceId}/members")]
    public async Task<IActionResult> ShowMembers([FromRoute] Guid workspaceId)
    {
        var result = await _mediator.Send(new ShowMembersRequest(workspaceId));
        return result.ToApiResult();
    }
}

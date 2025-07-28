using System;
using Microsoft.AspNetCore.Mvc;
using src.Domain.Enums;
using src.Feature.WorkspaceManager.AddMembers;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController 
{
    [HttpPost("{workspaceId}/members")]
    public async Task<IActionResult> AddMembers([FromRoute] Guid workspaceId, [FromBody] AddMembersBody body)
    {
        var request = new AddMembersRequest(workspaceId, body.Emails, body.Role);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

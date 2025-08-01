using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.LeaveWorkspace;
using src.Helper.Results;

namespace src.Feature.User;

public partial class UserController
{
    [HttpPost("leave-workspace")]
    public async Task<IActionResult> LeaveWorkspace([FromBody] Guid workspaceId)
    {
        var request = new LeaveWorkspaceRequest(workspaceId);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

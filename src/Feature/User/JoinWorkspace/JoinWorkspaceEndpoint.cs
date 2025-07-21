using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.JoinWorkspace;
using src.Helper.Results;

namespace src.Feature.User;

public partial class UserController
{
    [HttpPost("join-workspace")]
    public async Task<IActionResult> JoinWorkspace([FromBody] string joinCode)
    {
        var request = new JoinWorkspaceRequest(joinCode);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

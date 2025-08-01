using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.GetUserWorkspace;
using src.Helper.Results;

namespace src.Feature.User;

public partial class UserController
{
    [HttpGet("workspaces")]
    public async Task<IActionResult> GetUserWorkspaces()
    {
        var request = new GetUserWorkspaceRequest();
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}


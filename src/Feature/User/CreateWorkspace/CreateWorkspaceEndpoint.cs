using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.CreateWorkspace;
using src.Helper.Results;

namespace src.Feature.User;

public partial class UserController
{
    [HttpPost("workspace")]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

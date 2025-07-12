using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.Workspace.CreateWorkspace;
using src.Helper.Results;

namespace src.Feature.Workspace;

public partial class WorkspaceController
{
    [HttpPost()]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request,CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

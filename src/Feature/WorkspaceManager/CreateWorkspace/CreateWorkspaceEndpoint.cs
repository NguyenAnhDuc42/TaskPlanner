using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.CreateWorkspace;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpPost()]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request,CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

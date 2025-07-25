using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.Workspace.GetHierarchy;
using src.Helper.Results;

namespace src.Feature.Workspace;

public partial class WorkspaceController
{
    [HttpGet("{workspaceId}/hierarchy")]
    public async Task<IActionResult> GetHierarchy(Guid workspaceId, CancellationToken cancellationToken)
    {
        var request = new GetHierarchyRequest(workspaceId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

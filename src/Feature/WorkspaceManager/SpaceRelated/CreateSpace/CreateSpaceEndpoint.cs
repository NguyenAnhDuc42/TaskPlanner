using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.CreateSpace;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpPost("{workspaceId}/create-space")]
    public async Task<IActionResult> CreateSpace([FromRoute]Guid workspaceId,[FromBody] CreateSpaceBody body)
    {
        var request = new CreateSpaceRequest(workspaceId, body);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

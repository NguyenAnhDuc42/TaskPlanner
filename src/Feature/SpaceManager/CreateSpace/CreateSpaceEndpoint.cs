
using Microsoft.AspNetCore.Mvc;
using src.Feature.SpaceManager.CreateSpace;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpPost("{workspaceId}/spaces")]
    public async Task<IActionResult> CreateSpace([FromRoute]Guid workspaceId,[FromBody] CreateSpaceBody body)
    {
        var request = new CreateSpaceRequest(workspaceId, body);
        var spaceId = await _mediator.Send(request);
        return Created($"/api/workspaces/{workspaceId}/spaces/{spaceId}", spaceId);
    }
}
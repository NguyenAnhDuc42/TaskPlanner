using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.DeleteMembers;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpPost("{workspaceId}/delete-members")]
    public async Task<IActionResult> DeleteMembers(Guid workspaceId, List<Guid> memberIds)
    {
        var request = new DeleteMembersRequest(memberIds, workspaceId);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }

}

using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.WorkspaceManager.UpdateMembers;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager;

public partial class WorkspaceController
{
    [HttpPut("{workspaceId}/members")]
    public async Task<IActionResult> UpdateMembers([FromRoute] Guid workspaceId,[FromBody] UpdateMembersBody body)
    {
        var request = new UpdateMembersRequest(workspaceId, body.MemberIds, body.Role);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
       
    }
  
}

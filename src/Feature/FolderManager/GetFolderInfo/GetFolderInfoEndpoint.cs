using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using src.Feature.FolderManager.GetFolderInfo;
using src.Feature.SpaceManager.GetSpaceInfo;
using src.Helper.Results;

namespace src.Feature.FolderManager;

public partial class FolderController
{
    [HttpGet("{folderId:guid}/tasks")]
    public async Task<IActionResult> GetFolderTasks([FromRoute] Guid folderId, CancellationToken cancellationToken)
    {
        var request = new GetFolderInfoRequest(folderId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}
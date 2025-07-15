using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using src.Feature.ListManager.GetListInfo;
using src.Feature.SpaceManager.GetSpaceInfo;
using src.Helper.Results;

namespace src.Feature.ListManager;

public partial class ListController
{
    [HttpGet("{listId:guid}/tasks")]
    public async Task<IActionResult> GetListTasks([FromRoute] Guid listId, CancellationToken cancellationToken)
    {
        var request = new GetListInfoRequest(listId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}
using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.ListManager.GetTaskForList;
using src.Helper.Results;

namespace src.Feature.ListManager;

public partial class ListController
{
    [HttpGet("{listId:guid}/tasks")]
    public async Task<IActionResult> GetListTasks([FromRoute] Guid listId, CancellationToken cancellationToken)
    {
        var request = new GetTaskForListRequest(listId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}

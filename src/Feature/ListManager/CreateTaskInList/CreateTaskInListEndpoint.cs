using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.ListManager.CreateTaskInList;
using src.Helper.Results;

namespace src.Feature.ListManager;

public partial class ListController
{
    [HttpPost("createtask")]
    [Authorize(Policy = "CreateTask")]
    public async Task<IActionResult> Create(CreateTaskInListRequest request)
    {
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

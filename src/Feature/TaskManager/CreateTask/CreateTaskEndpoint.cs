using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.CreateTask;
using src.Helper.Results;

namespace src.Feature.TaskManager;

public partial class TasksController
{
    [HttpPost]
    [Authorize(Policy = "CreateTask")]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }

}

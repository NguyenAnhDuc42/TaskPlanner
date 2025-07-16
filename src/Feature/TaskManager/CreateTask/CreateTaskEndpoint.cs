using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.CreateTask;
using src.Helper.Results;

namespace src.Feature.TaskManager;

public partial class TaskController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }

}

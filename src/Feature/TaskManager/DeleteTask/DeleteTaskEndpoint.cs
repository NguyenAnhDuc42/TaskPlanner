using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.DeleteTask;
using src.Helper.Results;

namespace src.Feature.TaskManager;

public partial class TasksController
{
    [HttpDelete("{id}")]
    [Authorize(Policy = "DeleteTask")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeleteTaskRequest(id);
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }
}

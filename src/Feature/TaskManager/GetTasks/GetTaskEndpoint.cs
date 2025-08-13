

using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.GetTasks;
using src.Helper.Filters;

namespace src.Feature.TaskManager;

public partial class TasksController
{
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TaskQuery query, CancellationToken cancellationToken)
    {
        var request = new GetTasksRequest(query); // match your request shape
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

}

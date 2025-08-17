using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.GetTasksMetaData;
using src.Helper.Filters;

namespace src.Feature.TaskManager;

public partial class TasksController
{
    [HttpGet("metadata")]
    public async Task<IActionResult> GetTasksMetadata([FromQuery] TaskQuery query, CancellationToken cancellationToken)
    {
        var request = new GetTasksMetaDataRequest(query);
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.UpdateTask;
using src.Helper.Results;


namespace src.Feature.TaskManager
{
    public partial class TasksController
    {
        [HttpPut("{id}")]
        [Authorize(Policy = "EditTask")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskBodyRequest body)
        {
            var request = new UpdateTaskRequest(
                id,
                body.Name,
                body.Description,
                body.Priority,
                body.StartDate,
                body.DueDate,
                body.TimeEstimate,
                body.TimeSpent,
                body.OrderIndex,
                body.IsArchived,
                body.IsPrivate
            );
            var result = await _mediator.Send(request);
            return result.ToApiResult();
        }
    }
}

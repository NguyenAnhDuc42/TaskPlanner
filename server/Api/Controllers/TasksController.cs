using Microsoft.AspNetCore.Mvc;

namespace Api;

[Route("api/[controller]")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly IHandler _handler;

    public TasksController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync<CreateTaskCommand, Guid>(command, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var query = new GetTaskDetailQuery(id);
        var result = await _handler.QueryAsync<GetTaskDetailQuery, List<TaskRecord>>(query, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync(command with { TaskId = id }, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _handler.SendAsync(new DeleteTaskCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("{taskId:guid}/assignees")]
    public async Task<IActionResult> GetTaskAssignees(Guid taskId, CancellationToken ct)
    {
        var result = await _handler.QueryAsync<GetTaskAssigneesQuery, List<AssigneeRecord>>(new GetTaskAssigneesQuery(taskId), ct);
        return result.ToActionResult();
    }

    [HttpPut("{taskId:guid}/assignees")]
    public async Task<IActionResult> UpdateAssignees(Guid taskId, [FromBody] List<AssigneeChangeValue> changes, CancellationToken ct)
    {
        var command = new UpdateTaskAssigneesCommand(taskId, changes);
        var result = await _handler.SendAsync(command, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct)
    {
        var result = await _handler.QueryAsync<GetCommentsQuery, List<CommentRecord>>(new GetCommentsQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var result = await _handler.SendAsync<AddCommentCommand, CommentRecord>(new AddCommentCommand(id, request.Content, request.ParentCommentId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{parentTaskId:guid}/subtasks")]
    public async Task<IActionResult> CreateSubTask(Guid parentTaskId, [FromBody] CreateSubTaskCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync(command with { ParentTaskId = parentTaskId }, ct);
        return result.ToActionResult();
    }

    [HttpPut("{parentTaskId:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> UpdateSubTask(Guid parentTaskId, Guid subTaskId, [FromBody] UpdateSubTaskCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync(command with { ParentTaskId = parentTaskId, TaskId = subTaskId }, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{parentTaskId:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> DeleteSubTask(Guid parentTaskId, Guid subTaskId, [FromQuery] Guid spaceId, CancellationToken ct)
    {
        var result = await _handler.SendAsync(new DeleteSubTaskCommand(spaceId, parentTaskId, subTaskId), ct);
        return result.ToActionResult();
    }
}



using Microsoft.AspNetCore.Mvc;

namespace Api;

[Route("api/[controller]")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class TasksController : ControllerBase
{
    private readonly IHandler _handler;

    public TasksController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<CreateTaskCommand, Guid>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTaskDetailQuery(id);
        var result = await _handler.QueryAsync<GetTaskDetailQuery, List<TaskRecord>>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(command with { TaskId = id }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new DeleteTaskCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{taskId:guid}/assignees")]
    public async Task<IActionResult> GetTaskAssignees(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await _handler.QueryAsync<GetTaskAssigneesQuery, List<AssigneeRecord>>(new GetTaskAssigneesQuery(taskId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{taskId:guid}/assignees")]
    public async Task<IActionResult> UpdateAssignees(Guid taskId, [FromBody] List<AssigneeChangeValue> changes, CancellationToken cancellationToken)
    {
        var command = new UpdateTaskAssigneesCommand(taskId, changes);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _handler.QueryAsync<GetCommentsQuery, List<CommentRecord>>(new GetCommentsQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<AddCommentCommand, CommentRecord>(new AddCommentCommand(id, request.Content, request.ParentCommentId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new DeleteCommentCommand(id, commentId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{parentTaskId:guid}/subtasks")]
    public async Task<IActionResult> CreateSubTask(Guid parentTaskId, [FromBody] CreateSubTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(command with { ParentTaskId = parentTaskId }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{parentTaskId:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> UpdateSubTask(Guid parentTaskId, Guid subTaskId, [FromBody] UpdateSubTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(command with { ParentTaskId = parentTaskId, TaskId = subTaskId }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{parentTaskId:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> DeleteSubTask(Guid parentTaskId, Guid subTaskId, [FromQuery] Guid spaceId, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new DeleteSubTaskCommand(parentTaskId, subTaskId), cancellationToken);
        return result.ToActionResult();
    }
}

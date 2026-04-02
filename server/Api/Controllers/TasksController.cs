using Application.Features.TaskFeatures.SelfManagement.CreateTask;
using Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssigneeCandidates;
using Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssignees;
using Application.Features.TaskFeatures.AssigneeManagement.GetTaskListAssignees;
using Application.Features.TaskFeatures.SelfManagement.DeleteTask;
using Application.Features.TaskFeatures.SelfManagement.UpdateTask;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTaskCommand(
            TaskId: id,
            Name: request.Name,
            Description: request.Description,
            StatusId: request.StatusId,
            Priority: request.Priority,
            StartDate: request.StartDate,
            DueDate: request.DueDate,
            StoryPoints: request.StoryPoints,
            TimeEstimate: request.TimeEstimate,
            AssigneeIds: request.AssigneeIds
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }



    [HttpGet("{taskId:guid}/assignees")]
    public async Task<IActionResult> GetTaskAssignees(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaskAssigneesQuery(taskId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{taskId:guid}/assignee-candidates")]
    public async Task<IActionResult> GetTaskAssigneeCandidates(
        Guid taskId,
        [FromQuery] string? search,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTaskAssigneeCandidatesQuery(taskId, search, limit), cancellationToken);
        return Ok(result);
    }
}

public record UpdateTaskRequest(
    string? Name,
    string? Description,
    Guid? StatusId,
    Priority? Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    List<Guid>? AssigneeIds
);

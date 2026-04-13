using Application.Features.TaskFeatures.SelfManagement.CreateTask;
using Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssigneeCandidates;
using Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssignees;
using Application.Features.TaskFeatures.SelfManagement.DeleteTask;
using Application.Features.TaskFeatures.SelfManagement.UpdateTask;
using Application.Features.TaskFeatures.SelfManagement;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

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
        var result = await _handler.SendAsync<CreateTaskCommand, TaskDto>(command, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
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

        var result = await _handler.SendAsync<UpdateTaskCommand, TaskDto>(command, ct);
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
        var result = await _handler.QueryAsync<GetTaskAssigneesQuery, List<TaskAssigneeDto>>(new GetTaskAssigneesQuery(taskId), ct);
        return result.ToActionResult();
    }

    [HttpGet("{taskId:guid}/assignee-candidates")]
    public async Task<IActionResult> GetTaskAssigneeCandidates(
        Guid taskId,
        [FromQuery] string? search,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var result = await _handler.QueryAsync<GetTaskAssigneeCandidatesQuery, List<TaskAssigneeCandidateDto>>(
            new GetTaskAssigneeCandidatesQuery(taskId, search, limit), ct);
        return result.ToActionResult();
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

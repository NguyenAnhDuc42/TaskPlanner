using System;
using Domain.Enums;
using MediatR;

namespace Application.Features.TaskManagement.SelfManagement.CreateTask;

public record CreateTaskCommand(
    Guid ListId,
    string Name,
    string? Description,
    Guid? StatusId,
    Priority Priority,
    List<Guid>? AssigneeIds,  // Assign users immediately
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate
) : IRequest<Guid>;  // Return task ID

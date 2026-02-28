using Application.Common.Interfaces;
using System;
using Domain.Enums;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

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
) : ICommand<Guid>;  // Return task ID

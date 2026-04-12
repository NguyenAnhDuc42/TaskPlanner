using Application.Common.Interfaces;
using Domain.Enums;
using Application.Features.TaskFeatures.SelfManagement;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public record UpdateTaskCommand(
    Guid TaskId,
    string? Name,
    string? Description,
    Guid? StatusId,
    Priority? Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    List<Guid>? AssigneeIds = null
) : ICommandRequest<TaskDto>, IAuthorizedWorkspaceRequest;

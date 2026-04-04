using Application.Features.ViewFeatures.GetViewData;
using Application.Common.Interfaces;
using Domain.Enums;
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
) : ICommand<TaskDto>;

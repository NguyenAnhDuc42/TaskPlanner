using Application.Common.Interfaces;
using Application.Features.ViewFeatures.GetViewData;
using Domain.Enums;
using Domain.Enums.RelationShip;
namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public record CreateTaskCommand(
    Guid ParentId,
    EntityLayerType ParentType,
    string Name,
    string? Description,
    Guid? StatusId,
    Priority Priority,
    List<Guid>? AssigneeIds,  // Assign users immediately
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate
) : ICommand<TaskDto>;  // Return TaskDto

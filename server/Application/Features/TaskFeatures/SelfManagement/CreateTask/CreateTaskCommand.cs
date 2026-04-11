using Application.Common.Interfaces;
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
    List<Guid>? AssigneeIds,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate
) : ICommandRequest<TaskDto>;

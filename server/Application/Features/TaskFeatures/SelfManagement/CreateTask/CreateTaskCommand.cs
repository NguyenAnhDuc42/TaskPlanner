using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.TaskFeatures;

public record CreateTaskCommand(
    Guid ParentId,
    EntityLayerType ParentType,
    string Name,
    Guid? StatusId,
    Priority Priority,
    List<Guid>? AssigneeIds,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate
) : ICommandRequest<TaskDto>, IAuthorizedWorkspaceRequest;

public record TaskDto(
    Guid Id,
    Guid ProjectWorkspaceId,
    Guid? ProjectSpaceId,
    Guid? ProjectFolderId,
    string Name,
    Guid DefaultDocumentId,
    Guid? StatusId,
    Priority Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    string? OrderKey,
    DateTimeOffset CreatedAt,
    List<AssigneeDto> Assignees
);

public record AssigneeDto(
    Guid Id,
    string Name,
    string? AvatarUrl
);

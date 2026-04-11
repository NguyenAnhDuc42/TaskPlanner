using Domain.Enums;

namespace Application.Features.TaskFeatures.SelfManagement;

public record TaskDto(
    Guid Id,
    Guid ProjectWorkspaceId,
    Guid? ProjectSpaceId,
    Guid? ProjectFolderId,
    string Name,
    string? Description,
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

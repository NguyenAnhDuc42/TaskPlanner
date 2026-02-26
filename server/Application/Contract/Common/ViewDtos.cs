using Domain.Enums;

namespace Application.Contract.Common;

public record TaskDto(
    Guid Id,
    Guid ProjectListId,
    string Name,
    string? Description,
    Guid? StatusId,
    Priority Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    long? OrderKey
);

public record DocumentDto(
    Guid Id,
    Guid LayerId,
    string Name,
    string Content
);

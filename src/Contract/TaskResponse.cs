using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Domain.Enums;

namespace src.Contract;


public record class TaskDetail(
    Guid Id,
    string Name,
    string Description,
    Priority Priority,
    Status Status,
    DateTime? DueDate,
    DateTime? StartDate,
    long? TimeEstimate,
    long? TimeSpent,
    int OrderIndex,
    bool IsArchived,
    bool IsPrivate,
    Guid ListId,
    Guid CreatorId
);


public record class TaskSummary(Guid Id, string Name, DateTime? DueDate, Priority Priority, List<UserSummary> assignees);


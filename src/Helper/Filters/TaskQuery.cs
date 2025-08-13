using System.Text.Json.Serialization;
using src.Domain.Enums;

namespace src.Helper.Filters;

public record class TaskQuery
{
    // Hierarchy filters
    public Guid? WorkspaceId { get; init; }
    public Guid? SpaceId { get; init; }
    public Guid? FolderId { get; init; }
    public Guid? ListId { get; init; }
    public Guid? StatusId { get; init; }

    // User-related filters
    public Guid? AssigneeId { get; init; }
    public Guid? CreatorId { get; init; }
    public bool? CreatedByMe { get; init; } // Will be resolved to CreatorId in handler
    public bool? AssignedToMe { get; init; } // Will be resolved to AssigneeId in handler

    // Task attribute filters
    public Priority? Priority { get; init; }
    public List<Priority>? Priorities { get; init; } // For multiple priority filtering
    public DateTime? DueDateBefore { get; init; }
    public DateTime? DueDateAfter { get; init; }
    public DateTime? StartDateBefore { get; init; }
    public DateTime? StartDateAfter { get; init; }
    public bool? HasDueDate { get; init; }
    public bool? IsOverdue { get; init; }
    public bool? IsPrivate { get; init; }
    public bool? IsArchived { get; init; } = false; // Default to non-archived

    // Search and advanced filters
    public string? SearchTerm { get; init; }
    //public List<Guid>? TagIds { get; init; } If have tags in future which will probaly be implemented
    public long? TimeEstimateMin { get; init; }
    public long? TimeEstimateMax { get; init; }

    // Cursor pagination
    public string? Cursor { get; init; }
    public int PageSize { get; init; } = 50;
    public TaskSortBy SortBy { get; init; } = TaskSortBy.CreatedAt;
    public SortDirection Direction { get; init; } = SortDirection.Desc;

    // Performance optimization flags
    public bool IncludeAssignees { get; init; } = true;
    public bool IncludeTimeTracking { get; init; } = false;
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskSortBy
{
    CreatedAt,
    UpdatedAt,
    DueDate,
    Priority,
    Name,

    //OrderIndex future
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection { Asc, Desc }
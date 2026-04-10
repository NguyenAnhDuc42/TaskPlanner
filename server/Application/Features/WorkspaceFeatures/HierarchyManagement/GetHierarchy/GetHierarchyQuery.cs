using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public record class GetHierarchyQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceHierarchyDto>;

public record class WorkspaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public List<SpaceHierarchyDto> Spaces { get; init; } = new();
}

public record class SpaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
    public string OrderKey { get; init; } = null!;
    public List<FolderHierarchyDto> Folders { get; init; } = new();
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
}

public record class FolderHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
    public string OrderKey { get; init; } = null!;
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
}

public record class TaskHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid? StatusId { get; init; }
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public Priority Priority { get; init; }
    public string? OrderKey { get; init; }
}

// ── GetNodeTasks (lazy task loading on expand) ──────────────────────────

public record class GetNodeTasksQuery(
    Guid WorkspaceId,
    Guid ParentId,
    string ParentType,       // "Folder" or "Space"
    string? CursorOrderKey,  // null on first page
    string? CursorTaskId,    // null on first page
    int PageSize = 50
) : IQueryRequest<NodeTasksDto>;

public record class NodeTasksDto
{
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
    public string? NextCursorOrderKey { get; init; }
    public string? NextCursorTaskId { get; init; }
    public bool HasMore { get; init; }
}

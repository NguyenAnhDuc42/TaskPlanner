using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures;

public record GetHierarchyQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceHierarchyDto>, IAuthorizedWorkspaceRequest;

public record WorkspaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public List<SpaceHierarchyDto> Spaces { get; init; } = new();
}

public record SpaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
    public string OrderKey { get; init; } = null!;
    public bool HasFolders { get; init; }
    public bool HasTasks { get; init; }
    public List<FolderHierarchyDto> Folders { get; init; } = new();
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
}

public record FolderHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
    public string OrderKey { get; init; } = null!;
    public bool HasTasks { get; init; }
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
}

public record TaskHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid? StatusId { get; init; }
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public Priority Priority { get; init; }
    public string? OrderKey { get; init; }
}

// ── GetNodeFolders (lazy folder loading on expand) ──────────────────────────

public record GetNodeFoldersQuery(
    Guid WorkspaceId,
    Guid NodeId
) : IQueryRequest<List<FolderHierarchyDto>>, IAuthorizedWorkspaceRequest;


// ── GetNodeTasks (lazy task loading on expand) ──────────────────────────

public record GetNodeTasksQuery(
    Guid WorkspaceId,
    Guid ParentId,
    string ParentType,
    string? CursorOrderKey,
    string? CursorTaskId,
    int PageSize = 50
) : IQueryRequest<NodeTasksDto>, IAuthorizedWorkspaceRequest;

public record NodeTasksDto
{
    public List<TaskHierarchyDto> Tasks { get; init; } = new();
    public string? NextCursorOrderKey { get; init; }
    public string? NextCursorTaskId { get; init; }
    public bool HasMore { get; init; }
}

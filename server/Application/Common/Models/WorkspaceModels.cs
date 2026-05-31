namespace Application;

public record WorkspaceRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public string? Description { get; init; }
    
    // Member specific context (current user)
    public Role? Role { get; init; }
    public Theme? Theme { get; init; }
    public bool? IsPinned { get; init; }
    public bool? IsOwned { get; init; }
    
    // Security flags
    public bool? CanEdit { get; init; }
    public bool? CanInvite { get; init; }
    public bool? CanManageMembers { get; init; }
    public bool? CanPinWorkspace { get; init; }
    
    // General Stats
    public int? MemberCount { get; init; }
    public bool? IsArchived { get; init; }
    public bool? IsDashboardEnabled { get; init; }

}

public record MemberRecord 
{
    public Guid Id { get; init; }
    public Guid? WorkspaceMemberId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public Role? Role { get; init; }
    public MembershipStatus? Status { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }
}

public record WorkspaceHierarchyRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public List<SpaceRecord>? Spaces { get; init; } = null;
}

public record SpaceRecord
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }   // ancestor id for client-side workspace scoping
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsPrivate { get; init; }
    public string? OrderKey { get; init; }
    // Detailed properties
    public string? Description { get; init; }
    public Guid? ParentWorkflowId { get; init; }
    public Guid? WorkflowId { get; init; }
    public Guid? StatusId { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public Guid? CreatorId { get; init; }
    public List<Guid>? MemberIds { get; init; } = null;
    public bool? HasFolders { get; init; }
    public bool? HasTasks { get; init; }
}

public record EntityAccessRecord
{
    public Guid WorkspaceMemberId { get; init; }
    public AccessLevel AccessLevel { get; init; }
    public bool HaveAccess { get; init; }
}

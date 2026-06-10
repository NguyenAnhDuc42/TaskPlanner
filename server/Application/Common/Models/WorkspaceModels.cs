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

    public static WorkspaceRecord FromDomain(ProjectWorkspace w, WorkspaceMember? currentMember = null) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Icon = w.Icon,
        Color = w.Color,
        Description = w.Description,
        Role = currentMember?.Role,
        Theme = currentMember?.Theme,
        IsPinned = currentMember?.IsPinned,
        IsOwned = currentMember?.Role == Domain.Role.Owner,
        CanEdit = currentMember?.Role.IsAtLeast(Domain.Role.Admin),
        CanInvite = currentMember?.Role.IsAtLeast(Domain.Role.Admin),
        CanManageMembers = currentMember?.Role.IsAtLeast(Domain.Role.Admin),
        CanPinWorkspace = currentMember != null,
        MemberCount = null,
        IsArchived = w.IsArchived,
        IsDashboardEnabled = null
    };
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

    public static MemberRecord FromDomain(WorkspaceMember wm, User user) => new()
    {
        Id = user.Id,
        WorkspaceMemberId = wm.Id,
        Name = user.Name,
        Email = user.Email,
        Role = wm.Role,
        Status = wm.Status,
        CreatedAt = wm.CreatedAt,
        JoinedAt = wm.JoinedAt
    };
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
    public Guid? WorkflowId { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public Guid? CreatorId { get; init; }
    public bool? HasFolders { get; init; }
    public bool? HasTasks { get; init; }

    public static SpaceRecord FromDomain(ProjectSpace s,Guid? workflowId) => new()
    {
        Id = s.Id,
        WorkspaceId = s.ProjectWorkspaceId,
        Name = s.Name,
        Color = s.Color,
        Icon = s.Icon,
        IsPrivate = s.IsPrivate,
        OrderKey = s.OrderKey,
        WorkflowId = workflowId ,
        DefaultDocumentId = s.DefaultDocumentId,
        CreatedAt = s.CreatedAt,
        CreatorId = s.CreatorId,
        HasFolders = null,
        HasTasks = null
    };
}

public record EntityAccessRecord
{
    public Guid? Id { get; init; }
    public Guid? SpaceId { get; init; }
    public Guid WorkspaceMemberId { get; init; }
    public AccessLevel AccessLevel { get; init; }
    public bool HaveAccess { get; init; }

    public static EntityAccessRecord FromDomain(EntityAccess ea) => new()
    {
        Id = ea.Id,
        SpaceId = ea.ProjectSpaceId,
        WorkspaceMemberId = ea.WorkspaceMemberId,
        AccessLevel = ea.AccessLevel,
        HaveAccess = true
    };
}

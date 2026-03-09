namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record WorkspaceSecurityContextDto
{
    public Guid WorkspaceId { get; init; }
    public string CurrentRole { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public bool IsOwned { get; init; }
    public WorkspacePermissionFlagsDto PermissionFlags { get; init; } = new();
}

public record WorkspacePermissionFlagsDto
{
    public bool CanViewHierarchy { get; init; }
    public bool CanManageWorkspace { get; init; }
    public bool CanManageMembers { get; init; }
    public bool CanCreateSpace { get; init; }
    public bool CanUpdateWorkspace { get; init; }
    public bool CanDeleteWorkspace { get; init; }
}


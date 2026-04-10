using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceSecurityContextDto>;

public record WorkspaceSecurityContextDto
{
    public Guid WorkspaceId { get; init; }
    public string CurrentRole { get; init; } = string.Empty;
    public bool IsOwned { get; init; }
    public Domain.Enums.Workspace.Theme Theme { get; init; }
    public string Color { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    
    // Permission Flags
    public bool CanEdit { get; init; }
    public bool CanInvite { get; init; }
    public bool CanManageMembers { get; init; }
    
    // Feature Toggles
    public bool IsDashboardEnabled { get; init; }
}

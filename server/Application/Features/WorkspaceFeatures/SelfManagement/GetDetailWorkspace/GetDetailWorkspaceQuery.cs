using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceSecurityContextDto>, IAuthorizedWorkspaceRequest;

public record WorkspaceSecurityContextDto(
    Guid WorkspaceId,
    string CurrentRole,
    bool IsOwned,
    Domain.Enums.Workspace.Theme Theme,
    string Color,
    string Icon,
    bool CanEdit,
    bool CanInvite,
    bool CanManageMembers,
    bool IsDashboardEnabled
);

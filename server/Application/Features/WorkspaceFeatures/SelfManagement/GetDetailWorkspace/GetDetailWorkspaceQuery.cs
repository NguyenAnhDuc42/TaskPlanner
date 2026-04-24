using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceSecurityContextDto>, IAuthorizedWorkspaceRequest;

public record WorkspaceSecurityContextDto(
    Guid WorkspaceId,
    string CurrentRole,
    bool IsOwned,
    Theme Theme,
    string Color,
    string Icon,
    bool CanEdit,
    bool CanInvite,
    bool CanManageMembers,
    bool IsDashboardEnabled
);

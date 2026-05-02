using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums.Workspace;

namespace Application.Features.UserFeatures;

public record GetUserPreferenceQuery() : IQueryRequest<UserPreferenceDto>;

public record UserPreferenceDto(
    Guid UserId,
    Theme Theme,
    Guid? LastWorkspaceId,
    int SidebarWidth,
    bool SidebarCollapsed,
    string? LayoutData,
    Dictionary<Guid, WorkspaceSetting> WorkspaceSettings
);

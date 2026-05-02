using Application.Common.Interfaces;
using Domain.Enums.Workspace;

using Domain.Entities;

namespace Application.Features.UserFeatures;

public record UpdateUserPreferenceCommand(
    Theme? Theme,
    Guid? LastWorkspaceId,
    int? SidebarWidth,
    bool? SidebarCollapsed,
    string? LayoutData,
    Dictionary<Guid, WorkspaceSetting>? WorkspaceSettings
) : ICommandRequest;

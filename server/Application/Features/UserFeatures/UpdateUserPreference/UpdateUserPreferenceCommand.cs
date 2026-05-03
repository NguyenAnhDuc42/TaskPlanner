using Application.Common.Interfaces;
using Domain.Enums;

using Domain.Entities;

namespace Application.Features.UserFeatures;

public record UpdateUserPreferenceCommand(
    Theme? Theme,
    Guid? LastWorkspaceId,
    int? SidebarWidth,
    bool? SidebarCollapsed,
    string? LayoutData,
    Dictionary<Guid, WorkspaceSetting>? WorkspaceSettings,
    bool ClearLastWorkspaceId = false
) : ICommandRequest;

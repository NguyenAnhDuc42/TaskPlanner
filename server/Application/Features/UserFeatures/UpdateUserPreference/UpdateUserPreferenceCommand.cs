using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Entities;

namespace Application.Features.UserFeatures;

public record UpdateUserPreferenceCommand(
    [property: JsonPropertyName("theme")] Theme? Theme,
    [property: JsonPropertyName("lastWorkspaceId")] Guid? LastWorkspaceId,
    [property: JsonPropertyName("sidebarWidth")] int? SidebarWidth,
    [property: JsonPropertyName("sidebarCollapsed")] bool? SidebarCollapsed,
    [property: JsonPropertyName("layoutData")] string? LayoutData,
    [property: JsonPropertyName("workspaceSettings")] Dictionary<Guid, WorkspaceSetting>? WorkspaceSettings,
    [property: JsonPropertyName("clearLastWorkspaceId")] bool ClearLastWorkspaceId = false
) : ICommandRequest;

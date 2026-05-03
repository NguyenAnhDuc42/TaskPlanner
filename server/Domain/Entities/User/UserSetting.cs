using System;
using System.Collections.Generic;
using Domain.Enums;
using System.Text.Json.Serialization;

namespace Domain.Entities;

public class UserSetting 
{
    public Theme Theme { get; set; } = Theme.Dark;
    public Guid? LastWorkspaceId { get; set; }
    public int SidebarWidth { get; set; } = 280;
    public bool SidebarCollapsed { get; set; } = false;
    public string? LayoutData { get; set; }
    public Dictionary<Guid, WorkspaceSetting> WorkspaceSettings { get; set; } = new();
}

public class WorkspaceSetting
{
    [JsonPropertyName("sideBarWidth")]
    public int? SideBarWidth { get; set; }

    [JsonPropertyName("mainContentWidth")]
    public int? MainContentWidth { get; set; }

    [JsonPropertyName("contextContentWidth")]
    public int? ContextContentWidth { get; set; }  

    [JsonPropertyName("isSidebarOpen")]
    public bool IsSidebarOpen { get; set; } = true;
}
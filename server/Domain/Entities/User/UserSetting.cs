using Domain.Enums;

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
    public int? SideBarWidth { get; set; }
    public int? MainContentWidth { get; set; }
    public int? ContextContentWidth { get; set; }  
    public bool IsSidebarOpen { get; set; } = true;
}
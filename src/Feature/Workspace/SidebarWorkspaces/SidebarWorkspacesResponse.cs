namespace src.Feature.Workspace.SidebarWorkspaces;

public record SidebarWorkspacesResponse(List<SidebarWorkspace> Workspaces);

public record SidebarWorkspace(Guid Id, string Name, string Icon);
namespace src.Feature.Workspace.SidebarWorkspaces;

public record Workspaces(List<Workspace> workspaces);

public record Workspace(Guid Id, string Name);
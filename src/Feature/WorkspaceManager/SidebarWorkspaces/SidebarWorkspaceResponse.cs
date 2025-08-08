using src.Contract;

namespace src.Feature.WorkspaceManager.SidebarWorkspaces;

public record class GroupWorkspace(WorkspaceSummary currentWorkspace, IEnumerable<WorkspaceSummary> otherWorkspaces);
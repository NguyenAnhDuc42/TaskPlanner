using src.Application.Common.DTOs;

namespace src.Feature.WorkspaceManager.SidebarWorkspaces;

public record class GroupWorkspace(WorkspaceSummary currentWorkspace, IEnumerable<WorkspaceSummary> otherWorkspaces);
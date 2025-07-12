export interface CreateWorkspaceRequest {
  name: string;
  description: string;
  icon: string;
  color: string;
  isPrivate: boolean;
}
export interface CreateWorkspaceResponse {
  workspaceId: string;
  message: string;
}
export interface SidebarWorkspacesResponse {
  workspaces: SidebarWorkspace[];
}

export interface SidebarWorkspace {
  id: string;
  name: string;
  icon: string;
}

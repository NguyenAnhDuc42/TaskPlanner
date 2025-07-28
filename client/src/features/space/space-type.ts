export interface CreateSpaceRequest {
  workspaceId: string;
  name: string;
  icon?: string;
}

export interface CreateSpaceResponse {
  id: string;
  message: string;
}

export interface GetSpaceTasksRequest {
  spaceId: string;
}
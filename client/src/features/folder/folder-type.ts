export interface CreateFolderRequest {
  workspaceId: string;
  spaceId: string;
  name: string;
}

export interface CreateFolderResponse {
  id: string;
  message: string;
}

export interface GetFolderTasksRequest {
  folderId: string;
}


export interface CreateListRequest {
  workspaceId: string;
  spaceId: string;
  folderId?: string | null;
  name: string;
  icon?: string;
}

export interface CreateListResponse {
  id: string;
  message: string;
}

export interface GetListTasksRequest {
  listId: string;
}
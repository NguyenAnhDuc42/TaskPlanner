export interface FolderDetailDto {
  id: string;
  projectSpaceId: string;
  name: string;
  color?: string;
  icon?: string;
  isPrivate: boolean;
  isArchived: boolean;
  parentWorkflowId?: string;
  workflowId?: string;
  statusId?: string;
  defaultDocumentId?: string;
  startDate?: string;
  dueDate?: string;
  description?: string;
  memberIds: string[];
}

export interface UpdateFolderRequest {
  folderId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  statusId?: string;
  startDate?: string;
  dueDate?: string;
}

export interface EnrichedFolderDetailDto extends FolderDetailDto {
  status?: any;
  members: any[];
  assignees: any[];
}



export interface SpaceDetailDto {
  id: string;
  projectWorkspaceId: string;
  name: string;
  slug: string;
  color?: string;
  icon?: string;
  isPrivate: boolean;
  workflowId?: string;
  statusId?: string;
  defaultDocumentId?: string;
  startDate?: string;
  dueDate?: string;
  description?: string;
  memberIds: string[];
}

export interface UpdateSpaceRequest {
  spaceId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  statusId?: string;
  startDate?: string;
  dueDate?: string;
}

export interface EnrichedSpaceDetailDto extends SpaceDetailDto {
  status?: any;
  members: any[];
  assignees: any[];
}

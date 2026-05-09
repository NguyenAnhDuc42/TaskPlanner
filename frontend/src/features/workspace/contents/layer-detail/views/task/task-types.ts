export interface TaskDetailDto {
  id: string;
  projectFolderId: string;
  projectSpaceId?: string;
  name: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  description?: string;
  icon?: string;
  color?: string;
  assigneeIds: string[];
}

export interface UpdateTaskRequest {
  taskId: string;
  name?: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
}

export interface EnrichedTaskDetailDto extends TaskDetailDto {
  status?: any;
  members: any[];
  assignees: any[];
}

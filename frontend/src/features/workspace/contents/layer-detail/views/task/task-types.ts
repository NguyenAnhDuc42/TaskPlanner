import type { Priority } from "@/types/priority";

export interface TaskDetailDto {
  id: string;
  projectFolderId: string;
  projectSpaceId?: string;
  name: string;
  statusId?: string;
  priority?: Priority;
  startDate?: string;
  dueDate?: string;
  description?: string;
  icon?: string;
  color?: string;
  assigneeIds: string[];
  defaultDocumentId: string;
}

export interface UpdateTaskRequest {
  taskId: string;
  name?: string;
  statusId?: string;
  priority?: Priority;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
  icon?: string;
  color?: string;
}

export interface EnrichedTaskDetailDto extends TaskDetailDto {
  status?: any;
  members: any[];
  assignees: any[];
}

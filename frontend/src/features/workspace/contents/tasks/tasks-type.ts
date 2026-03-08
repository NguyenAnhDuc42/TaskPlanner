import type { Priority } from "@/types/priority";


export interface AssigneeDto {
  id: string;
  name: string;
  avatarUrl?: string;
}

export interface TaskDto {
  id: string;
  projectListId: string;
  name: string;
  description?: string;
  statusId?: string;
  priority: Priority;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  orderKey?: number;
  createdAt: string;
  assignees: AssigneeDto[];
}

export interface CreateTaskRequest {
  listId: string;
  name: string;
  description?: string;
  statusId?: string;
  priority?: Priority;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
}

export interface TaskCreateListOption {
  id: string;
  name: string;
  color: string;
  icon: string;
}

export interface TaskAssigneeOption {
  userId: string;
  userName: string;
  avatarUrl?: string;
}

export interface UpdateTaskRequest {
  taskId: string;
  name?: string;
  description?: string;
  statusId?: string;
  priority?: Priority;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
}

export interface StatusDto {
  id: string;
  name: string;
  color: string;
  category: string;
  isDefault: boolean;
}

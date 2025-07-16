import { Task } from "@/types/task";


export interface CreateTaskRequest {
  name: string;
  description: string;
  priority: number;
  startDate?: string | null;
  dueDate?: string | null;
  isPrivate: boolean;
  workspaceId: string;
  spaceId: string;
  folderId?: string | null;
  listId: string;
}

export interface CreateTaskResponse {
  id: string;
  message: string;
}

export interface UpdateTaskBodyRequest {
  name?: string;
  description?: string;
  priority?: number;
  startDate?: string | null;
  dueDate?: string | null;
  timeEstimate?: number | null;
  timeSpent?: number | null;
  orderIndex?: number;
  isArchived?: boolean;
  isPrivate?: boolean;
  listId?: string;
}
export interface UpdateTaskResponse {
  task: Task;
  message: string;
}

export interface DeleteTaskResponse{
    message: string;
}


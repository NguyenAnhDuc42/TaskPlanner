import { PlanTaskStatus } from "@/types/task";


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

export interface CreateTaskInListRequest {
  name: string
  description?: string 
  priority: number
  status: PlanTaskStatus
  startDate?: string | null
  dueDate?: string | null
  isPrivate: boolean
  listId: string
}


export interface TaskLineList {
  tasks: Record<PlanTaskStatus, TaskLineItem[]>;
}
export interface TaskLineItem {
  id: string;
  name: string;
  priority: number;
  status: PlanTaskStatus;
  startDate?: string | null;
  dueDate?: string | null;
}
export interface CreateSpaceRequest {
  workspaceId: string;
  name: string;
  icon?: string;
}

export interface CreateSpaceResponse {
  id: string;
  message: string;
}

// Types for fetching tasks within a space, folder, or list
export interface Task {
  id: string;
  name: string;
  priority: string;
  startDate: string | null;
  dueDate: string | null;
}

export interface TaskList{
  tasks: Task[];
}

export interface GetSpaceTasksRequest {
  spaceId: string;
}
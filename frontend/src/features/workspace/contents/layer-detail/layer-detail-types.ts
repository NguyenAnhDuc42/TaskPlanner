import { Priority } from "@/types/priority";
import { StatusCategory } from "@/types/status-category";

export type MainViewTab = "overview" | "items";
export type ItemsViewMode = "board" | "list";

export interface TaskItemDto {
  id: string;
  name: string;
  createdAt: string;
  statusId?: string;
  priority?: Priority;
  dueDate?: string;
  startDate?: string;
}

export interface FolderItemDto {
  id: string;
  name: string;
  createdAt: string;
  statusId?: string;
  startDate?: string;
  dueDate?: string;
  icon?: string;
  color?: string;
}

export interface StatusDto {
  statusId: string;
  name: string;
  color: string;
  category: StatusCategory;
}

export interface TaskViewData {
  folders: FolderItemDto[];
  tasks: TaskItemDto[];
  statuses: StatusDto[];
  progress: {
    completedTasks: number;
    totalTasks: number;
  };
  recentActivity: Array<{
    id: string;
    content: string;
    timestamp: string;
  }>;
  startDate?: string;
  dueDate?: string;
  status?: {
    name: string;
    color: string;
  };
  workflowName?: string;
}

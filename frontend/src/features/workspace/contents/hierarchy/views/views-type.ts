import type { Priority } from "@/types/priority";
import type { StatusCategory } from "@/types/status-category";
import { ViewType } from "@/types/view-type";

// ==========================================
// Entities
// ==========================================

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
}

export interface TaskItemStatusDto {
  statusId: string;
  name: string;
  color: string;
  category: StatusCategory;
}

export interface DocumentDto {
  id: string;
  name: string;
  type?: string;
  extension?: string;
  sizeBytes?: number;
  createdAt: string;
}

// ==========================================
// View Definition & Configuration
// ==========================================

export interface ViewDto {
  id: string;
  name: string;
  viewType: ViewType;
  isDefault: boolean;
  filterConfigJson?: string;
  displayConfigJson?: string;
}

// ==========================================
// Overview Specific Data
// ==========================================

export interface OverviewStatusDto {
  name: string;
  category: string;
  color: string;
}

export interface OverviewProgressDto {
  completedTasks: number;
  totalTasks: number;
}

export interface OverviewActivityDto {
  id: string;
  content: string;
  type: string;
  timestamp: string;
}

export interface OverviewTimeDto {
  timeEstimate?: string;
  timeLogged?: string;
  remainingTime?: string;
}

export interface OverviewStatsDto {
  totalTasks: number;
  totalFolders: number;
}

export interface OverviewViewData {
  id: string;
  name: string;
  color?: string;
  icon?: string;
  description?: string;
  status?: OverviewStatusDto;
  workflowName?: string;
  progress: OverviewProgressDto;
  recentActivity: OverviewActivityDto[];
  stats: OverviewStatsDto;
  startDate?: string;
  dueDate?: string;
  timeStats?: OverviewTimeDto;
}

// ==========================================
// View Data Responses
// ==========================================

export interface TaskViewData {
  folders: FolderItemDto[];
  tasks: TaskItemDto[];
  statuses: TaskItemStatusDto[];
}

export interface AssetViewData {
  items: DocumentDto[];
  totalCount: number;
}

export interface ViewResponse {
  viewId: string;
  viewType: ViewType;
  data: TaskViewData | AssetViewData | OverviewViewData;
}

import type { Priority } from "@/types/priority";
import type { StatusCategory } from "@/types/status-category";
import { ViewType } from "@/types/view-type";

// ==========================================
// Shared Enums (matching backend)
// ==========================================




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
}

export interface FolderItemDto {
  id: string;
  name: string;
  createdAt: string;
  workflowId?: string;
  statusId?: string;
}

export interface ExplorerStatusGroupDto {
  statusId: string;
  statusName: string;
  category: StatusCategory;
  color: string;
  folders: FolderItemDto[];
  tasks: TaskItemDto[];
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
  viewType:ViewType; // Matching backend ViewType enum
  isDefault: boolean;
  filterConfigJson?: string;
  displayConfigJson?: string;
}

// ==========================================
// View Data Responses
// ==========================================

export interface TaskViewData {
  groups: ExplorerStatusGroupDto[];
}

export interface AssetViewData {
  items: DocumentDto[];
  totalCount: number;
}

export interface OverviewViewData {
  id: string;
  name: string;
  description?: string;
  statusId?: string;
  workflowId?: string;
  chatRoomId?: string;
  creatorId: string;
  createdAt: string;
  stats: {
    totalTasks: number;
    totalFolders: number;
  };
}

export interface ViewResponse {
  viewId: string;
  viewType: string;
  data: TaskViewData | AssetViewData | OverviewViewData;
}

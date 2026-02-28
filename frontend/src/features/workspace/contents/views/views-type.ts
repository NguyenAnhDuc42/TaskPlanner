import { ViewType } from "@/types/view-type";

// ==========================================
// Entities
// ==========================================
export interface TaskDto {
  id: string;
  projectListId: string;
  name: string;
  description?: string;
  statusId?: string;
  priority: string; // Maps to enum Priority
  startDate?: string; // ISO date string
  dueDate?: string; // ISO date string
  storyPoints?: number;
  timeEstimate?: number;
  orderKey?: number;
  createdAt: string; // ISO date string
}

export interface DocumentDto {
  id: string;
  layerId: string;
  name: string;
  content: string;
}

export interface StatusDto {
  id: string;
  name: string;
  color: string;
  category: string; // Maps to StatusCategory enum
  isDefault: boolean;
}

// ==========================================
// View Definition & Configuration
// ==========================================
export interface DisplayConfig {
  groupBy?: string;
  sortBy?: string;
  visibleColumns?: string[];
}

export interface FilterConfig {
  filters?: { field: string; operator: string; value: unknown }[];
}

export interface ViewDto {
  id: string;
  name: string;
  viewType: ViewType;
  isDefault: boolean;
  filterConfigJson?: string;
  displayConfigJson?: string;
}

// ==========================================
// Polymorphic View Results
// ==========================================
export interface BaseViewResult {
  viewType: ViewType;
}

export interface TaskListViewResult extends BaseViewResult {
  viewType: typeof ViewType.List;
  tasks: TaskDto[];
  statuses: StatusDto[];
}

export interface TasksBoardViewResult extends BaseViewResult {
  viewType: typeof ViewType.Board;
  tasks: TaskDto[];
  statuses: StatusDto[];
}

export interface DocumentListResult extends BaseViewResult {
  viewType: typeof ViewType.Doc;
  documents: DocumentDto[];
  statuses: StatusDto[];
}

export type ViewResponse =
  | TaskListViewResult
  | TasksBoardViewResult
  | DocumentListResult;

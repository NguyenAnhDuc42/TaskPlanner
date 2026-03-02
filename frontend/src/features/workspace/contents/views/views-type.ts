import { ViewType } from "@/types/view-type";
import type { TaskDto, StatusDto } from "../tasks/tasks-type";

export type { TaskDto, StatusDto };

// ==========================================
// Entities
// ==========================================

export interface DocumentDto {
  id: string;
  layerId: string;
  name: string;
  content: string;
}

// StatusDto is now imported from tasks-type for consistency

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

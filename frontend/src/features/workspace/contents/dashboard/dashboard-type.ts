import type { EntityLayerType } from "@/types/relationship-type";
export type { PagedResult } from "@/types/paged-result";
import { WidgetType } from "@/types/widget-type";

export interface DashboardDto {
  id: string;
  name: string;
  isShared: boolean;
  isMain: boolean;
  layerType: EntityLayerType;
  layerId: string;
}

export interface WidgetLayoutDto {
  col: number;
  row: number;
  width: number;
  height: number;
}

export interface WidgetDto {
  id: string;
  dashboardId: string;
  layout: WidgetLayoutDto;
  widgetType: WidgetType;
  configJson: string;
  visibility: WidgetVisibility;
}

export const WidgetVisibility = {
  Public: 0,
  Private: 1,
} as const;

export type WidgetVisibility = (typeof WidgetVisibility)[keyof typeof WidgetVisibility];

export interface CreateDashboardRequest {
  layerType: EntityLayerType;
  layerId: string;
  name: string;
  isShared: boolean;
  isMain: boolean;
}

export interface CreateWidgetRequest {
  dashboardId: string;
  widgetType: WidgetType;
  Col: number;
  Row: number;
  Width: number;
  Height: number;
}

export interface SaveDashboardLayoutRequest {
  WidgetId: string;
  Col: number;
  Row: number;
  Width: number;
  Height: number;
}

// --- Dynamic Widget Data (SignalR Pushes) ---

export interface WidgetPosition {
  col: number;
  row: number;
  width: number;
  height: number;
}

export interface WidgetDataBase {
  widgetId: string;
  position: WidgetPosition;
  type: WidgetType; // Correct type
}

export interface TaskStatusItem {
  id: string;
  listId: string;
  title: string;
  statusId: string;
  startDate: string | null;
  dueDate: string | null;
  priority: number;
  createdAt: string;
}

export interface TaskStatusWidgetData extends WidgetDataBase {
  type: typeof WidgetType.TaskList; // Using enum/const value
  totalCount: number;
  overdueCount: number;
  todayCount: number;
  tasks: TaskStatusItem[];
}

export type WidgetData = TaskStatusWidgetData;

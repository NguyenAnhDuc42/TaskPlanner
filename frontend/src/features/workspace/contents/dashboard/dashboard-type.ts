
import type { EntityLayerType } from "@/types/relationship-type";
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

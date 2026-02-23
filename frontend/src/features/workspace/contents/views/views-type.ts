import { ViewType } from "@/types/view-type";

export interface ViewDto {
  id: string;
  name: string;
  viewType: ViewType;
  isDefault: boolean;
}

export interface ViewResponse {
  tasks: any[];
  statuses: any[];
  // Additional view meta depending on type
  widgets?: any[];
  documentContent?: string;
}

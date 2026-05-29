import { StatusCategory } from "./status-category";

export interface Status {
  id: string;
  workflowId: string;
  name: string;
  color: string;
  category: StatusCategory;
  orderKey: string;
}

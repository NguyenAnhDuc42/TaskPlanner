import { StatusCategory } from "./status-category";

export interface Status {
  statusId: string;
  name: string;
  color: string;
  category: StatusCategory;
}

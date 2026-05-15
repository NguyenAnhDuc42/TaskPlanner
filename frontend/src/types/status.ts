import { StatusCategory } from "./status-category";

export interface Status {
  id: string;
  name: string;
  color: string;
  category: StatusCategory;
}

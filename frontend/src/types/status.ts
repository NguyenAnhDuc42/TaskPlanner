import { StatusCategory } from "./status-category";

export interface StatusDto {
  id: string;
  name: string;
  color: string;
  category: StatusCategory;
}

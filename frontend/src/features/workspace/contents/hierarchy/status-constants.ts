import { StatusCategory } from "./status-types";

export interface CategoryMetadata {
  id: StatusCategory;
  label: string;
  color: string;
  bgColor: string;
}

export const STATUS_CATEGORIES: CategoryMetadata[] = [
  {
    id: StatusCategory.NotStarted,
    label: "Not Started",
    color: "text-muted-foreground",
    bgColor: "bg-muted/30",
  },
  {
    id: StatusCategory.Active,
    label: "Active",
    color: "text-blue-500",
    bgColor: "bg-blue-500/10",
  },
  {
    id: StatusCategory.Done,
    label: "Done",
    color: "text-green-500",
    bgColor: "bg-green-500/10",
  },
  {
    id: StatusCategory.Closed,
    label: "Closed",
    color: "text-emerald-500",
    bgColor: "bg-emerald-500/10",
  },
];

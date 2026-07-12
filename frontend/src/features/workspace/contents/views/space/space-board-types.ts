import type { TaskRecord } from "@/types/projects";
import { prioritySort } from "@/types/priority";

export interface SpaceBoardFilter {
  priorities?: string[];
  statusIds?: string[];
  search?: string;
  startDate?: string;
  dueDate?: string;
}

export type BoardItem = TaskRecord & { __type: "task" };

export type SpaceBoardSortBy = "priority" | "manual" | "createdNewest" | "createdOldest" | "dueDate" | "name";

export const SORT_OPTIONS: { value: SpaceBoardSortBy; label: string }[] = [
  { value: "priority", label: "Priority" },
  { value: "manual", label: "Manual" },
  { value: "createdNewest", label: "Created (Newest)" },
  { value: "createdOldest", label: "Created (Oldest)" },
  { value: "dueDate", label: "Due Date" },
  { value: "name", label: "Name (A–Z)" },
];

export function getBoardSortComparator(sortBy: SpaceBoardSortBy) {
  switch (sortBy) {
    case "manual":
      return (a: BoardItem, b: BoardItem) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1);
    case "createdNewest":
      return (a: BoardItem, b: BoardItem) => (b.createdAt ?? "").localeCompare(a.createdAt ?? "");
    case "createdOldest":
      return (a: BoardItem, b: BoardItem) => (a.createdAt ?? "").localeCompare(b.createdAt ?? "");
    case "dueDate":
      return (a: BoardItem, b: BoardItem) => {
        if (!a.dueDate && !b.dueDate) return 0;
        if (!a.dueDate) return 1;
        if (!b.dueDate) return -1;
        return a.dueDate.localeCompare(b.dueDate);
      };
    case "name":
      return (a: BoardItem, b: BoardItem) => a.name.localeCompare(b.name);
    case "priority":
    default:
      return prioritySort;
  }
}

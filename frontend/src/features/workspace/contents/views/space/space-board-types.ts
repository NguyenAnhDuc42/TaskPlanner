import type { TaskRecord } from "@/types/projects";

export interface SpaceBoardFilter {
  priorities?: string[];
  folderIds?: string[]; // "__none__" = tasks with no folder (direct space tasks)
  search?: string;
  startDate?: string;
  dueDate?: string;
}

export type BoardItem = TaskRecord & { __type: "task"; folderName?: string };

import { UserSummary } from "./user";

export type PlanTaskStatus  = "ToDo" | "InProgress" | "InReview" | "Done"

export interface TaskDetail {
  id: string;
  name: string;
  description: string;
  priority: number;
  status: PlanTaskStatus;
  dueDate: string | null;
  startDate: string | null;
  timeEstimate: number | null;
  timeSpent: number | null;
  orderIndex: number;
  isArchived: boolean;
  isPrivate: boolean;
  listId: string;
  creatorId: string;
}

export interface TaskSummary {
  id: string;
  name: string;
  priority: number;
  dueDate: string | null;
  assignees: UserSummary[];
}

export interface Assignee {
    id: string;
    name: string;
    email: string;
}
export interface TaskItem {
    id: string;
    name: string;
    dueDate: string | null;
    status: PlanTaskStatus;
    priority: number;
    assignees: Assignee[];
}

export interface TaskItems {
    tasks: TaskItem[];
}
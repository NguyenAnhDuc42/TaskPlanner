import { Priority } from "@/utils/priority-utils";
import { UserSummary } from "./user";

export interface TaskDetail {
  id: string;
  name: string;
  description: string;
  priority: Priority;
  status: Status;
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
export interface Status {
  id: string;
  name: string;
  color: string;
  type: StatusType;
}
export enum StatusType {
  NotStarted,
  Active,
  Done,
  Closed,
}

export interface TaskSummary {
  id: string;
  name: string;
  priority: Priority;
  dueDate: string | null;
  assignees: UserSummary[];
}


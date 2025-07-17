export type PlanTaskStatus  = "ToDo" | "InProgress" | "InReview" | "Done"

export interface Task {
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

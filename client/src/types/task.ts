import { Priority } from "@/utils/priority-utils";
import { UserSummary } from "./user";

// Corresponds to: public record class TaskSummary(Guid Id, ...)
export interface TaskSummary {
  id: string;
  name: string;
  dueDate: string | null;
  priority: Priority;
  assignees: UserSummary[];
}

// Corresponds to: public record class TasksSummary(List<TaskSummary> tasks)
export interface TasksSummary {
  tasks: TaskSummary[];
}
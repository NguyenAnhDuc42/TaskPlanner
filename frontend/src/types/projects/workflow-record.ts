import type { Status } from "@/types/status";

export interface WorkflowRecord {
  id: string;
  name: string;
  workspaceId: string;
  statuses?: Status[];
}

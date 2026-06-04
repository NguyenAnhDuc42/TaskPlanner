export interface AssigneeRecord {
  id: string; // Composite ID: `${taskId}_${workspaceMemberId}`
  taskId: string;
  workspaceMemberId: string;
}

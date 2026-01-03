import type { Role } from "@/types/role";
import type { WorkspaceVariant } from "@/types/workspace-variant";



export interface WorkspaceSummary {
  id: string;
  name: string;
  icon: string;
  color: string;
  variant: WorkspaceVariant;
  role: Role;
  memberCount: number;
  isPinned: boolean; // included since your DTO has it
  updatedAt?: string; // optional — useful for sorting/cursor
}
import type { AccessLevel } from "@/types/access-level";

export interface FolderRecord {
    id: string;
    workspaceId?: string;
    spaceId?: string;
    name: string;
    createdAt: string;
    startDate?: string | null;
    dueDate?: string | null;
    orderKey?: string;
    icon?: string;
    color?: string;
    hasTasks?: boolean;
    accessLevel?: AccessLevel;
}

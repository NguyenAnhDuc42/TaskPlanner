import { Priority } from "../priority";

export interface FolderRecord {
    id: string;
    workspaceId?: string;   // ancestor: workspace
    spaceId?: string;       // ancestor: space (was parentId)
    name: string;
    createdAt: string; // ISO 8601 string
    statusId?: string;
    priority?: Priority;
    startDate?: string; // ISO 8601 string
    dueDate?: string;   // ISO 8601 string
    orderKey?: string;
    icon?: string;
    color?: string;
    isPrivate?: boolean;
    hasTasks?: boolean;
}

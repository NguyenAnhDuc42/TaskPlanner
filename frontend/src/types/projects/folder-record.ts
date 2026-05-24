import { Priority } from "../priority";

export interface FolderRecord {
    id: string;
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
    parentId?: string;
}

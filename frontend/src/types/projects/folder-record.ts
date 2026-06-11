import { Priority } from "../priority";

export interface FolderRecord {
    id: string;
    workspaceId?: string;
    spaceId?: string;
    name: string;
    createdAt: string;
    statusId?: string;
    priority?: Priority;
    startDate?: string;
    dueDate?: string;
    orderKey?: string;
    icon?: string;
    color?: string;
    isPrivate?: boolean;
    hasTasks?: boolean;
    workflowId?: string;  
}

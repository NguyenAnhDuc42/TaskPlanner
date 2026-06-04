import { Priority } from "../priority";

export interface TaskRecord {
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
    spaceId?: string | null;
    folderId?: string | null;
    description?: string;
    parentWorkflowId?: string;
    defaultDocumentId?: string;
    isArchived?: boolean;
    storyPoints?: number;
    timeEstimateSeconds?: number;
    parentType?: string;
}


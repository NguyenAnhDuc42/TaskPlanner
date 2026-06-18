import { Priority } from "../priority";

export interface TaskRecord {
    id: string;
    workspaceId?: string;
    name: string;
    createdAt: string;
    statusId?: string;
    priority?: Priority;
    startDate?: string | null;
    dueDate?: string | null;
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
    parentTaskId?: string;
    isFavorite?: boolean;
}


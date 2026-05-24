export interface SpaceRecord {
    id: string;
    name: string;
    color?: string;
    icon?: string;
    isPrivate: boolean;
    orderKey?: string;
    description?: string;
    parentWorkflowId?: string;
    workflowId?: string;
    statusId?: string;
    defaultDocumentId?: string;
    startDate?: string; // ISO 8601 string
    dueDate?: string;   // ISO 8601 string
    createdAt?: string; // ISO 8601 string
    memberIds?: string[];
    hasFolders?: boolean;
    hasTasks?: boolean;
}

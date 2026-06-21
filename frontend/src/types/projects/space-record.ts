import type { AccessLevel } from "@/types/access-level";

export interface SpaceRecord {
    id: string;
    workspaceId?: string;
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
    startDate?: string;
    dueDate?: string;
    createdAt?: string;
    memberIds?: string[];
    hasFolders?: boolean;
    hasTasks?: boolean;
    creatorId?: string;
    accessLevel?: AccessLevel;
    isFavorite?: boolean;
    favoriteOrderKey?: string;
}

import { Priority } from "../priority";
import type { AccessLevel } from "@/types/access-level";
import type { EntityLayerType } from "@/types/entity-layer-type";

export interface TaskRecord {
    id: string;
    workspaceId?: string;
    name: string;
    createdAt: string;
    statusId?: string | null;
    priority?: Priority;
    startDate?: string | null;
    dueDate?: string | null;
    orderKey?: string;
    icon?: string;
    color?: string;
    spaceId?: string | null;
    folderId?: string | null;
    description?: string;
    defaultDocumentId?: string;
    isArchived?: boolean;
    storyPoints?: number;
    timeEstimateSeconds?: number;
    parentType?: EntityLayerType;
    parentTaskId?: string;
    isFavorite?: boolean;
    favoriteOrderKey?: string;
    accessLevel?: AccessLevel;
}


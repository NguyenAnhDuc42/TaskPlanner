import type { EntityLayerType } from "@/types/entity-layer-type";

export interface FavoriteRecord {
    id: string;
    entityId: string;
    entityLayerType: EntityLayerType;
    orderKey: string;
    workspaceId: string;
    name?: string;
    icon?: string;
    color?: string;
    spaceId?: string;
    folderId?: string;
}

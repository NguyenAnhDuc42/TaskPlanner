import type { EntityLayerType } from "@/types/entity-layer-type";

export interface FavoriteRecord {
  id: string;
  entityId: string;
  entityLayerType: EntityLayerType;
  orderKey: string;
}

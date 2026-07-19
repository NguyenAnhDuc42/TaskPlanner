import { EntityLayerType } from "@/types/entity-layer-type";
import type { DocumentRecord } from "@/types/projects";

export type DragDocumentData = DocumentRecord & { type: typeof EntityLayerType.ProjectDocument; id: string; parentId: string | null; spaceId: string };

export type DragItemData = DragDocumentData;

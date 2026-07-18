import { EntityLayerType } from "@/types/entity-layer-type";
import type { SpaceRecord, FolderRecord, TaskRecord, DocumentRecord } from "@/types/projects";

export type DragSpaceData = SpaceRecord & { type: typeof EntityLayerType.ProjectSpace; id: string; orderKey?: string };
export type DragFolderData = FolderRecord & { type: typeof EntityLayerType.ProjectFolder; id: string; parentId: string; spaceId: string };
export type DragTaskData = TaskRecord & { type: typeof EntityLayerType.ProjectTask; id: string; parentId: string; parentType: EntityLayerType; spaceId: string };
export type DragDocumentData = DocumentRecord & { type: typeof EntityLayerType.ProjectDocument; id: string; parentId: string | null; spaceId: string };

export type DragItemData = DragSpaceData | DragFolderData | DragTaskData | DragDocumentData;

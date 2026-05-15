import { type EntityLayerType } from "@/types/entity-layer-type";

export interface WorkspaceHierarchy {
  id: string;
  name: string;
  slug: string;
  spaces: SpaceHierarchy[];
}

export interface SpaceHierarchy {
  id: string;
  name: string;
  slug: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  orderKey: string;
  hasFolders: boolean;
  hasTasks: boolean;
  folders: FolderHierarchy[];
  tasks: TaskHierarchy[];
}

export interface FolderHierarchy {
  id: string;
  name: string;
  slug: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  orderKey: string;
  hasTasks: boolean;
  tasks: TaskHierarchy[];
}

export interface TaskHierarchy {
  id: string;
  name: string;
  slug: string;
  statusId?: string;
  priority: number;
  color?: string;
  icon?: string;
  orderKey: string;
}

export interface NodeTasksResponse {
  tasks: TaskHierarchy[];
  nextCursorOrderKey?: string;
  nextCursorTaskId?: string;
  hasMore: boolean;
}

// Request DTOs (Preserved Space/Folder/Task for Sidebar Creation)
export interface CreateSpaceRequest {
  workspaceId: string;
  name: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  memberIdsToInvite?: string[];
}

export interface CreateFolderRequest {
  spaceId: string;
  name: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  statusId?: string | null;
  startDate?: string;
  dueDate?: string;
}

export interface CreateTaskRequest {
  parentId: string;
  parentType: EntityLayerType;
  name: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  color?: string;
  icon?: string;
}

export interface MoveItemRequest {
  itemId: string;
  itemType: EntityLayerType;
  targetParentId?: string;
  previousItemOrderKey?: string;
  nextItemOrderKey?: string;
  newOrderKey?: string;
}

export type HierarchyDndData = (SpaceHierarchy | FolderHierarchy | TaskHierarchy) & {
  id: string;
  type: EntityLayerType;
  parentId?: string;
  parentType?: EntityLayerType;
};

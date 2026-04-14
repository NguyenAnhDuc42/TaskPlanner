import { type EntityLayerType } from "@/types/entity-layer-type";

export interface WorkspaceHierarchy {
  id: string;
  name: string;
  slug: string;
  description?: any; // JSONB
  spaces: SpaceHierarchy[];
}

export interface SpaceHierarchy {
  id: string;
  name: string;
  slug: string;
  description?: any; // JSONB
  color: string;
  icon: string;
  isPrivate: boolean;
  orderKey: string;
  folders: FolderHierarchy[];
  tasks: TaskHierarchy[];
}

export interface FolderHierarchy {
  id: string;
  name: string;
  slug: string;
  description?: any; // JSONB
  color: string;
  icon: string;
  isPrivate: boolean;
  orderKey: string;
  tasks: TaskHierarchy[];
}

export interface TaskHierarchy {
  id: string;
  name: string;
  slug: string;
  description?: any; // JSONB
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

// Request DTOs (Preserved Space/Folder, Purged List)
export interface CreateSpaceRequest {
  workspaceId: string;
  name: string;
  description?: string;
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
}

export interface CreateTaskRequest {
  parentId: string;
  parentType: EntityLayerType;
  name: string;
  description?: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
}

export interface UpdateSpaceRequest {
  spaceId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
}

export interface UpdateFolderRequest {
  folderId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
}

export interface UpdateTaskRequest {
  taskId: string;
  name?: string;
  description?: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
}

// Access DTOs (Preserved Space/Folder/Task, Purged List)
export interface EntityAccessMember {
  workspaceMemberId: string;
  fullName: string;
  avatarUrl?: string;
  email: string;
  explicitAccess: string | null;
  effectiveAccess: string;
  isCreator: boolean;
  isInherited: boolean;
}

export interface UpdateEntityAccessBulkRequest {
  entityId: string;
  layerType: number; // 1: Space, 2: Folder, 4: Task (List is dead)
  members: {
    workspaceMemberId: string;
    accessLevel?: string;
    isRemove: boolean;
  }[];
}

export interface MoveItemRequest {
  itemId: string;
  itemType: EntityLayerType;
  targetParentId?: string;
  previousItemOrderKey?: string;
  nextItemOrderKey?: string;
  newOrderKey?: string;
}

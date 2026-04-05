export interface WorkspaceHierarchy {
  id: string;
  name: string;
  spaces: SpaceHierarchy[];
}

export interface SpaceHierarchy {
  id: string;
  name: string;
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
  color: string;
  icon: string;
  isPrivate: boolean;
  orderKey: string;
  tasks: TaskHierarchy[];
}

export interface TaskHierarchy {
  id: string;
  name: string;
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
  parentType: 'Space' | 'Folder';
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
  itemType: "Space" | "Folder" | "Task";
  targetParentId?: string;
  previousItemOrderKey?: string;
  nextItemOrderKey?: string;
}

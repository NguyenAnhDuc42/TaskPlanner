import { Priority } from "@/types/priority";
import { StatusCategory } from "@/types/status-category";
import { type EntityLayerType } from "@/types/entity-layer-type";

export type MainViewTab = "overview" | "items";
export type ItemsViewMode = "board" | "list";

export interface TaskItemDto {
  id: string;
  name: string;
  createdAt: string;
  statusId?: string;
  priority?: Priority;
  dueDate?: string;
  startDate?: string;
  isPrivate?: boolean;
  orderKey?: string;
}

export interface FolderItemDto {
  id: string;
  name: string;
  createdAt: string;
  statusId?: string;
  startDate?: string;
  dueDate?: string;
  orderKey?: string;
  icon?: string;
  color?: string;
}

export interface StatusDto {
  statusId: string;
  name: string;
  color: string;
  category: StatusCategory;
}

export interface FolderDetailDto {
  id: string;
  projectSpaceId: string;
  name: string;
  color?: string;
  icon?: string;
  isPrivate: boolean;
  isArchived: boolean;
  parentWorkflowId?: string;
  workflowId?: string;
  statusId?: string;
  defaultDocumentId?: string;
  startDate?: string;
  dueDate?: string;
  description?: string;
  memberIds: string[];
}

export interface SpaceDetailDto {
  id: string;
  projectWorkspaceId: string;
  name: string;
  slug: string;
  color?: string;
  icon?: string;
  isPrivate: boolean;
  workflowId?: string;
  statusId?: string;
  defaultDocumentId?: string;
  startDate?: string;
  dueDate?: string;
  description?: string;
  memberIds: string[];
}

export interface TaskDetailDto {
  id: string;
  projectFolderId: string;
  name: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  description?: string;
  icon?: string;
  color?: string;
  assigneeIds: string[];
  isPrivate?: boolean;
}

export interface TaskViewData {
  folders: FolderItemDto[];
  tasks: TaskItemDto[];
  statuses: StatusDto[];
  progress: {
    completedTasks: number;
    totalTasks: number;
  };
  recentActivity: Array<{
    id: string;
    content: string;
    timestamp: string;
  }>;
  startDate?: string;
  dueDate?: string;
  status?: {
    name: string;
    color: string;
  };
  workflowName?: string;
}

// --- Hierarchy Types for Layer Logic (Decoupled) ---

export interface LayerFolderItem {
  id: string;
  name: string;
  icon: string;
  color: string;
}

export interface LayerSpaceItem {
  id: string;
  name: string;
  icon: string;
  color: string;
  folders: LayerFolderItem[];
}

export interface LayerHierarchy {
  spaces: LayerSpaceItem[];
}

// --- Request DTOs ---

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

export interface UpdateSpaceRequest {
  spaceId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  statusId?: string;
  startDate?: string;
  dueDate?: string;
}

export interface UpdateFolderRequest {
  folderId: string;
  name?: string;
  color?: string;
  icon?: string;
  isPrivate?: boolean;
  statusId?: string | null;
  startDate?: string;
  dueDate?: string;
}

export interface UpdateTaskRequest {
  taskId: string;
  name?: string;
  statusId?: string;
  priority?: number;
  startDate?: string;
  dueDate?: string;
  storyPoints?: number;
  timeEstimate?: number;
  assigneeIds?: string[];
}

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
  layerType: number; 
  members: {
    workspaceMemberId: string;
    accessLevel?: string;
    isRemove: boolean;
  }[];
}


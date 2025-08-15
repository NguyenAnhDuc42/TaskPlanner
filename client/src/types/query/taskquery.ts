import { Priority } from '@/utils/priority-utils';

export enum TaskSortBy {
  CreatedAt = 'CreatedAt',
  UpdatedAt = 'UpdatedAt',
  DueDate = 'DueDate',
  Priority = 'Priority',
  Name = 'Name',
}

export enum SortDirection {
  Asc = 'Asc',
  Desc = 'Desc',
}

export interface TaskQuery {
  // Hierarchy filters
  workspaceId?: string;
  spaceId?: string;
  folderId?: string;
  listId?: string;
  statusId?: string;

  // User-related filters
  assigneeId?: string;
  creatorId?: string;
  createdByMe?: boolean;
  assignedToMe?: boolean;

  // Task attribute filters
  priority?: Priority | null;
  priorities?: Priority[] | null;
  dueDateBefore?: string | null; // Dates are typically strings in ISO format
  dueDateAfter?: string | null;
  startDateBefore?: string | null;
  startDateAfter?: string | null;
  hasDueDate?: boolean | null;
  isOverdue?: boolean | null;
  isPrivate?: boolean | null;
  isArchived?: boolean;

  // Search and advanced filters
  searchTerm?: string | null;
  timeEstimateMin?: number | null;
  timeEstimateMax?: number | null;

  // Cursor pagination
  cursor?: string | null;
  pageSize?: number;
  sortBy?: TaskSortBy;
  direction?: SortDirection;

  // Performance optimization flags
  includeAssignees?: boolean;
  includeTimeTracking?: boolean;
}

import { api } from "@/lib/api-client";
import { useMutation, useInfiniteQuery, useQuery } from "@tanstack/react-query";
import type { PagedResult } from "@/types/paged-result";
import type { TaskRecord } from "@/types/projects/task-record";
import type { Status } from "@/types/status";

import type { FolderRecord } from "@/types/projects/folder-record";

export interface FolderDetailResponse {
  folder: FolderRecord;
  statuses: Status[];
  workflowId?: string;
}

export interface TaskFilter {
  statusIds?: string[];
  priorities?: string[];
  assigneeIds?: string[];
  startDate?: string;
  dueDate?: string;
  search?: string;
}

export interface GetFolderTasksRequest {
  cursor?: string | null;
  limit?: number;
  filter?: TaskFilter;
}

export interface BatchUpdateFolderTaskValue {
  id: string;
  statusId?: string | null;
  priority?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  orderKey?: string | null;
  isDeleted?: boolean | null;
}

export async function getFolderDetail(folderId: string): Promise<FolderDetailResponse> {
  const { data } = await api.get(`/folders/${folderId}`);
  return data;
}

export const folderQueryOptions = {
  detail: (folderId: string) => ({
    queryKey: ["folderDetail", folderId],
    queryFn: () => getFolderDetail(folderId),
  }),
  tasks: (folderId: string, filter?: TaskFilter, limit = 50) => ({
    queryKey: ["folderTasks", folderId, filter],
    queryFn: ({ pageParam }: { pageParam?: string | null }) => getFolderTasks(folderId, { cursor: pageParam, limit, filter }),
    getNextPageParam: (lastPage: PagedResult<TaskRecord>) => (lastPage.hasNextPage ? lastPage.nextCursor : undefined),
    initialPageParam: undefined as string | undefined | null,
  }),
};

export function useGetFolderDetail(folderId: string) {
  return useQuery(folderQueryOptions.detail(folderId));
}

export async function getFolderTasks(folderId: string, req: GetFolderTasksRequest): Promise<PagedResult<TaskRecord>> {
  const { data } = await api.post(`/folders/${folderId}/tasks`, req);
  return data;
}

export function useGetFolderTasks(folderId: string, filter?: TaskFilter, limit = 50) {
  return useInfiniteQuery(folderQueryOptions.tasks(folderId, filter, limit));
}

export async function batchUpdateFolderTasks(folderId: string, updates: BatchUpdateFolderTaskValue[]): Promise<void> {
  await api.post(`/folders/${folderId}/tasks/batch`, { folderId, updates });
}

export function useBatchUpdateFolderTasks(folderId: string) {
  return useMutation({
    mutationFn: (updates: BatchUpdateFolderTaskValue[]) => batchUpdateFolderTasks(folderId, updates),
  });
}


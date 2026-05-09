import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "@/features/main/query-keys";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import type { FolderDetailDto, UpdateFolderRequest } from "./folder-types";

export const folderQueryOptions = {
  detail: (workspaceId: string, folderId: string) => ({
    queryKey: [...workspaceKeys.all, "folder", folderId],
    queryFn: async () => {
      const { data } = await api.get<FolderDetailDto>(`/folders/${folderId}`);
      return data;
    },
    enabled: !!workspaceId && !!folderId,
    staleTime: 3000,
  })
};

export function useFolderDetail(workspaceId: string, folderId: string, enabled = true) {
  const { registry } = useWorkspace();
  const query = useQuery({
    ...folderQueryOptions.detail(workspaceId, folderId),
    enabled: enabled && !!workspaceId && !!folderId
  });

  const enrichedData = useMemo(() => {
    if (!query.data) return null;
    const data = query.data;
    const status = data.statusId ? registry.statusMap[data.statusId] : null;
    const members = (data.memberIds || []).map((id: string) => registry.memberMap[id]).filter(Boolean);

    return {
      ...data,
      status,
      members,
      assignees: members
    };
  }, [query.data, registry]);

  return {
    data: enrichedData,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error
  };
}

export function useUpdateFolder(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateFolderRequest) => api.put(`/folders/${data.folderId}`, data),
    onMutate: async (updates) => {
      await queryClient.cancelQueries({ queryKey: [...workspaceKeys.all, "folder", updates.folderId] });
      const previousDetail = queryClient.getQueryData([...workspaceKeys.all, "folder", updates.folderId]);
      if (previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "folder", updates.folderId], (old: any) => ({
          ...old,
          ...updates
        }));
      }
      return { previousDetail };
    },
    onSuccess: () => {
      onSuccess?.();
    },
    onError: (_err, updates, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "folder", updates.folderId], context.previousDetail);
      }
    },
  });
}

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "@/features/main/query-keys";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import type { SpaceDetailDto, UpdateSpaceRequest } from "./space-types";
import type { TaskViewData } from "../../layer-detail-types";

export const spaceQueryOptions = {
  detail: (workspaceId: string, spaceId: string) => ({
    queryKey: [...workspaceKeys.all, "space", spaceId],
    queryFn: async () => {
      const { data } = await api.get<SpaceDetailDto>(`/spaces/${spaceId}`);
      return data;
    },
    enabled: !!workspaceId && !!spaceId,
    staleTime: 3000,
  }),
  items: (spaceId: string) => ({
    queryKey: [...workspaceKeys.all, "space", spaceId, "items"],
    queryFn: async () => {
      const { data } = await api.get<TaskViewData>(`/spaces/${spaceId}/items`);
      return data;
    },
    enabled: !!spaceId,
    staleTime: 3000,
  })
};

export function useSpaceDetail(workspaceId: string, spaceId: string, enabled = true) {
  const { registry } = useWorkspace();
  const query = useQuery({
    ...spaceQueryOptions.detail(workspaceId, spaceId),
    enabled: enabled && !!workspaceId && !!spaceId
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

export function useSpaceItems(spaceId: string) {
  return useQuery(spaceQueryOptions.items(spaceId));
}

export function useUpdateSpace(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSpaceRequest) => api.put(`/spaces/${data.spaceId}`, data),
    onMutate: async (updates) => {
      await queryClient.cancelQueries({ queryKey: [...workspaceKeys.all, "space", updates.spaceId] });
      const previousDetail = queryClient.getQueryData([...workspaceKeys.all, "space", updates.spaceId]);
      if (previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "space", updates.spaceId], (old: any) => ({
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
        queryClient.setQueryData([...workspaceKeys.all, "space", updates.spaceId], context.previousDetail);
      }
    },
  });
}

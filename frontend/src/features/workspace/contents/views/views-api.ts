import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type { ViewDto, ViewResponse } from "./views-type";
import { ViewType } from "@/types/view-type";

export const useViews = (layerId: string, layerType: string) => {
  return useQuery({
    queryKey: ["views", layerId, layerType],
    queryFn: async () => {
      const response = await api.get<ViewDto[]>(
        `/views?layerId=${layerId}&layerType=${layerType}`,
      );
      return response.data;
    },
    enabled: !!layerId,
  });
};

export const useViewData = (viewId: string) => {
  return useQuery({
    queryKey: ["viewData", viewId],
    queryFn: async () => {
      const response = await api.get<ViewResponse>(`/views/${viewId}/data`);
      return response.data;
    },
    enabled: !!viewId,
  });
};

export const useCreateView = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      layerId: string;
      layerType: string;
      name: string;
      viewType: ViewType;
    }) => {
      const response = await api.post("/views", data);
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: ["views", variables.layerId, variables.layerType],
      });
    },
  });
};

export const useUpdateView = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      id: string;
      layerId: string;
      layerType: string;
      name?: string;
      isDefault?: boolean;
      filterConfigJson?: string;
      displayConfigJson?: string;
    }) => {
      // The API only needs the ID in the URL and the rest in the body
      const { id, layerId: _layerId, layerType: _layerType, ...body } = data;
      const response = await api.put(`/views/${id}`, { id, ...body });
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: ["views", variables.layerId, variables.layerType],
      });
    },
  });
};

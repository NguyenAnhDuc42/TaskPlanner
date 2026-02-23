import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type { ViewDto, ViewResponse } from "./views-type";
import { ViewType } from "@/types/view-type";

export const useViews = (layerId: string, layerType: string) => {
  return useQuery({
    queryKey: ["views", layerId, layerType],
    queryFn: async () => {
      const response = await api.get<ViewDto[]>(
        `/api/views?layerId=${layerId}&layerType=${layerType}`,
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
      const response = await api.get<ViewResponse>(`/api/views/${viewId}/data`);
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
      const response = await api.post("/api/views", data);
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: ["views", variables.layerId, variables.layerType],
      });
    },
  });
};

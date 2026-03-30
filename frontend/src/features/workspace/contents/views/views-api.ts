import { useQuery, useMutation, useQueryClient, queryOptions } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type { ViewDto, ViewResponse } from "./views-type";
import { ViewType } from "@/types/view-type";
import { viewsKeys } from "./views-keys";
import { EntityLayerType } from "@/types/relationship-type";

export const viewsQueryOptions = {
  list: (layerId: string, layerType: EntityLayerType) =>
    queryOptions({
      queryKey: viewsKeys.list(layerId, layerType),
      queryFn: async () => {
        const response = await api.get<ViewDto[]>( `/views?layerId=${layerId}&layerType=${layerType}`,);
        return response.data;
      },
      enabled: !!layerId,
    }),
  data: (viewId: string) =>
    queryOptions({
      queryKey: viewsKeys.data(viewId),
      queryFn: async () => { const response = await api.get<ViewResponse>(`/views/${viewId}/data`);
        return response.data;
      },
      enabled: !!viewId,
    }),
};

export const useViews = (layerId: string, layerType: EntityLayerType) => {
  return useQuery(viewsQueryOptions.list(layerId, layerType));
};

export const useViewData = (viewId: string) => {
  return useQuery(viewsQueryOptions.data(viewId));
};

export const useCreateView = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      layerId: string;
      layerType: EntityLayerType;
      name: string;
      viewType: ViewType;
    }) => {
      const response = await api.post("/views", data);
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: viewsKeys.list(variables.layerId, variables.layerType),
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
      layerType: EntityLayerType;
      name?: string;
      isDefault?: boolean;
      filterConfigJson?: string;
      displayConfigJson?: string;
    }) => {
      const { id, layerId: _layerId, layerType: _layerType, ...body } = data;
      const response = await api.put(`/views/${id}`, { id, ...body });
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: viewsKeys.list(variables.layerId, variables.layerType),
      });
    },
  });
};

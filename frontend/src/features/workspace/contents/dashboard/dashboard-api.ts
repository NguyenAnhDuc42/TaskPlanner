import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";

import type { 
  DashboardDto, 
  WidgetDto, 
  CreateDashboardRequest, 
  CreateWidgetRequest, 
  SaveDashboardLayoutRequest 
} from "./dashboard-type";
import type { EntityLayerType } from "@/types/relationship-type";
import type { PagedResult } from "@/types/paged-result";

export const dashboardKeys = {
  all: ["dashboards"] as const,
  list: (layerId: string, layerType: EntityLayerType) => [...dashboardKeys.all, "list", layerId, layerType] as const,
  widgets: (dashboardId: string) => [...dashboardKeys.all, "widgets", dashboardId] as const,
};

export const dashboardQueryOptions = {
  list: (layerId: string, layerType: EntityLayerType) => ({
    queryKey: dashboardKeys.list(layerId, layerType),
    queryFn: async () => {
      const { data } = await api.get<PagedResult<DashboardDto>>("/dashboards", {
        params: { layerId, layerType },
      });
      return data;
    },
    enabled: !!layerId,
  }),
  widgets: (dashboardId: string) => ({
    queryKey: dashboardKeys.widgets(dashboardId),
    queryFn: async () => {
      const { data } = await api.get<PagedResult<WidgetDto>>(`/dashboards/${dashboardId}/widgets`);
      return data;
    },
    enabled: !!dashboardId,
  }),
};

export function useDashboards(layerId: string, layerType: EntityLayerType) {
  return useQuery(dashboardQueryOptions.list(layerId, layerType));
}

export function useWidgets(dashboardId: string) {
  return useQuery(dashboardQueryOptions.widgets(dashboardId));
}

export function useCreateDashboard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateDashboardRequest) => {
      await api.post("/dashboards", request);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: dashboardKeys.list(variables.layerId, variables.layerType),
      });
    },
  });
}

export function useCreateWidget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateWidgetRequest) => {
      await api.post(`/dashboards/${request.dashboardId}/widgets`, request);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: dashboardKeys.widgets(variables.dashboardId),
      });
    },
  });
}

export function useSaveDashboardLayout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ dashboardId, layouts }: { dashboardId: string; layouts: SaveDashboardLayoutRequest[] }) => {
      await api.post(`/dashboards/${dashboardId}/layout`, layouts);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: dashboardKeys.widgets(variables.dashboardId),
      });
    },
  });
}

export function useDeleteDashboard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, layerId, layerType }: { id: string; layerId: string; layerType: EntityLayerType }) => {
      await api.delete(`/dashboards/${id}`, {
        params: { layerId, layerType },
      });
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: dashboardKeys.list(variables.layerId, variables.layerType),
      });
    },
  });
}

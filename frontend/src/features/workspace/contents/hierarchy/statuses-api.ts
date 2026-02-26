import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { type StatusDto, type StatusCategory } from "./status-types";
import { statusKeys } from "./status-keys";

export const useStatuses = (layerId: string, layerType: string) => {
  return useQuery({
    queryKey: statusKeys.list(layerId, layerType),
    queryFn: async () => {
      const response = await api.get<StatusDto[]>(
        `/statuses?layerId=${layerId}&layerType=${layerType}`,
      );
      return response.data;
    },
    enabled: !!layerId,
  });
};

export const useCreateStatus = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      layerId: string;
      layerType: string;
      name: string;
      color: string;
      category: StatusCategory;
    }) => {
      const response = await api.post("/statuses", data);
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: statusKeys.list(variables.layerId, variables.layerType),
      });
    },
  });
};

export const useUpdateStatus = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      id,
      ...data
    }: Partial<StatusDto> & { id: string }) => {
      await api.put(`/statuses/${id}`, { id, ...data });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statusKeys.all });
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
    },
  });
};

export const useDeleteStatus = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/statuses/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statusKeys.all });
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
    },
  });
};
export const useSyncStatuses = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      layerId: string;
      layerType: string;
      statuses: Array<{
        id?: string;
        name: string;
        color: string;
        category: StatusCategory;
        isDeleted: boolean;
      }>;
    }) => {
      await api.post("/statuses/sync", data);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: statusKeys.list(variables.layerId, variables.layerType),
      });
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
    },
  });
};

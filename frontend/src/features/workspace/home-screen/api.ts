import type z from "zod";
import type { createWorkspaceSchema, WorkspaceSummary } from "./type";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "../query-keys";

import { toast } from "sonner";
import { isAxiosError } from "axios";

type CreateWorkspaceValues = z.infer<typeof createWorkspaceSchema>;

export function useCreateWorkspace() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (values: CreateWorkspaceValues) => {
      const result = await api.post("/workspace", {
        ...values,
      });
      return result.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: workspaceKeys.list() });
      toast.success("Workspace created successfully");
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        const message =
          data.detail || data.title || "Failed to create workspace";
        toast.error(message);
      } else {
        toast.error("An unexpected error occurred");
      }
    },
  });
}

export function useWorkspaces() {
  return useQuery({
    queryKey: workspaceKeys.list(),
    queryFn: async () => {
      const { data } = await api.get<{ items: WorkspaceSummary[] }>(
        "/workspace"
      );
      return data.items;
    },
    staleTime: 1000 * 60 * 2,
    refetchOnWindowFocus: false,
    placeholderData: keepPreviousData,
  });
}

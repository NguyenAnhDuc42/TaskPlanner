import type z from "zod";
import type { createWorkspaceSchema, WorkspaceSummary } from "./type";
import {
  keepPreviousData,
  useMutation,
  useInfiniteQuery,
  useQueryClient,
  infiniteQueryOptions,
} from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "../query-keys";

import { toast } from "sonner";
import { isAxiosError } from "axios";
import type { PagedResult } from "@/types/paged-result";

type CreateWorkspaceValues = z.infer<typeof createWorkspaceSchema>;

export function useCreateWorkspace() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (values: CreateWorkspaceValues) => {
      const result = await api.post("/workspaces", {
        ...values,
      });
      return result.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workspaceKeys.list() });
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

export const workspaceInfiniteQueryOptions = (
  filters: {
    name?: string;
    owned?: boolean;
    isArchived?: boolean;
    variant?: string;
    direction?: "Ascending" | "Descending";
  } = {},
) =>
  infiniteQueryOptions({
    queryKey: [...workspaceKeys.list(), filters],
    queryFn: async ({ pageParam }: { pageParam: string | null }) => {
      const { data } = await api.get<PagedResult<WorkspaceSummary>>(
        "/workspaces",
        {
          params: {
            cursor: pageParam,
            ...filters,
          },
        },
      );
      return data;
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor || undefined,
  });

export function useWorkspaces(filters?: {
  name?: string;
  owned?: boolean;
  isArchived?: boolean;
  variant?: string;
  direction?: "Ascending" | "Descending";
}) {
  return useInfiniteQuery({
    ...workspaceInfiniteQueryOptions(filters),
    staleTime: 1000 * 60 * 2,
    refetchOnWindowFocus: false,
    placeholderData: keepPreviousData,
  });
}

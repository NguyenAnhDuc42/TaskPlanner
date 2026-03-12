import { z } from "zod";
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
import { useNavigate, useSearch } from "@tanstack/react-router";
import React from "react";
import { signalRService } from "@/lib/signalr-service";

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

export function useSetWorkspacePin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: { workspaceId: string; isPinned: boolean }) => {
      await api.put(`/workspaces/${payload.workspaceId}/pin`, {
        isPinned: payload.isPinned,
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workspaceKeys.list() });
    },
    onError: () => {
      toast.error("Failed to update workspace pin");
    },
  });
}

export function useJoinWorkspaceByCode() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (joinCode: string) => {
      const response = await api.post<{
        workspaceId: string;
        membershipStatus: string;
        isNewMember: boolean;
      }>("/workspaces/join", { joinCode });
      return response.data;
    },
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: workspaceKeys.list() });
      if (data.membershipStatus === "Pending") {
        toast.info("Join request sent. Waiting for approval.");
      } else {
        toast.success("Joined workspace successfully");
      }
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        const message = data.detail || data.title || "Failed to join workspace";
        toast.error(message);
      } else {
        toast.error("Failed to join workspace");
      }
    },
  });
}

export function useWorkspaceHome() {
  const search = useSearch({ from: "/" }) as any;
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    data,
    isLoading: isWorkspacesLoading,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useWorkspaces(search);

  const workspaces = React.useMemo(() => {
    return data?.pages.flatMap((page) => page.items) ?? [];
  }, [data]);

  const { mutate: createInternal, isPending: isCreating } =
    useCreateWorkspace();
  const { mutate: setWorkspacePin } = useSetWorkspacePin();

  const [isCreateModalOpen, setIsCreateModalOpen] = React.useState(false);
  const [isJoinModalOpen, setIsJoinModalOpen] = React.useState(false);

  React.useEffect(() => {
    signalRService.startConnection();

    const onUpdate = () => {
      queryClient.invalidateQueries({ queryKey: workspaceKeys.list() });
    };

    signalRService.on("WorkspaceUpdated", onUpdate);
    signalRService.on("WorkspacePinned", onUpdate);

    return () => {
      signalRService.off("WorkspaceUpdated", onUpdate);
      signalRService.off("WorkspacePinned", onUpdate);
    };
  }, [queryClient]);

  const handleCreateWorkspace = React.useCallback(
    (data: any) => {
      createInternal({ ...data, strictJoin: false });
    },
    [createInternal],
  );

  const handleJoinWorkspace = React.useCallback(() => {
    setIsJoinModalOpen(true);
  }, []);

  const handlePinWorkspace = React.useCallback(
    (workspaceId: string, isPinned: boolean) => {
      setWorkspacePin({ workspaceId, isPinned });
    },
    [setWorkspacePin],
  );

  const handleSearchChange = React.useCallback(
    (name: string) => {
      (navigate as any)({
        search: (prev: any) => ({ ...prev, name: name || undefined }),
        replace: true,
      });
    },
    [navigate],
  );

  const handleFilterChange = React.useCallback(
    (newFilters: any) => {
      (navigate as any)({
        search: (prev: any) => ({ ...prev, ...newFilters }),
      });
    },
    [navigate],
  );

  return {
    filters: search,
    workspaces,
    isWorkspacesLoading,
    isCreating,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isJoinModalOpen,
    setIsJoinModalOpen,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    handleCreateWorkspace,
    handleJoinWorkspace,
    handlePinWorkspace,
    handleSearchChange,
    handleFilterChange,
  };
}

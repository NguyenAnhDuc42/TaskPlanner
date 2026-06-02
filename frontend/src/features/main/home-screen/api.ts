import { z } from "zod";
import type { createWorkspaceSchema, WorkspaceSummary } from "./type";
import { workspaceApi } from "@/store/workspaceApi";
import { toast } from "sonner";
import type { PagedResult } from "@/types/paged-result";
import { useNavigate, useSearch } from "@tanstack/react-router";
import React from "react";
import { signalRService } from "@/lib/signalr-service";

type CreateWorkspaceValues = z.infer<typeof createWorkspaceSchema>;

export const homeWorkspaceApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkspaces: build.query<PagedResult<WorkspaceSummary>, {
      name?: string;
      owned?: boolean;
      isArchived?: boolean;
      variant?: string;
      direction?: "Ascending" | "Descending";
      cursor?: string | null;
    }>({
      query: (params) => ({ url: "/workspaces", method: "GET", params }),
      // Infinite scroll: reuse cache based only on filters
      serializeQueryArgs: ({ endpointName, queryArgs }) => {
        const { cursor, ...filters } = queryArgs;
        return `${endpointName}_${JSON.stringify(filters)}`;
      },
      merge: (currentCache, newItems, { arg }) => {
        if (!arg.cursor) {
          return newItems;
        }
        currentCache.items.push(...newItems.items);
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg !== previousArg;
      },
      providesTags: ["Spaces"],
    }),
    createWorkspace: build.mutation<WorkspaceSummary, CreateWorkspaceValues & { strictJoin?: boolean }>({
      query: (values) => ({ url: "/workspaces", method: "POST", data: values }),
      invalidatesTags: ["Spaces"],
    }),
    setWorkspacePin: build.mutation<void, { workspaceId: string; isPinned: boolean }>({
      query: (payload) => ({ url: `/workspaces/${payload.workspaceId}/pin`, method: "PUT", data: { isPinned: payload.isPinned } }),
      invalidatesTags: ["Spaces"],
    }),
    joinWorkspace: build.mutation<{
      workspaceId: string;
      membershipStatus: string;
      isNewMember: boolean;
    }, string>({
      query: (joinCode) => ({ url: "/workspaces/join", method: "POST", data: { joinCode } }),
      invalidatesTags: ["Spaces"],
    }),
  }),
});

export const {
  useGetWorkspacesQuery,
  useCreateWorkspaceMutation,
  useSetWorkspacePinMutation,
  useJoinWorkspaceMutation,
} = homeWorkspaceApi;

export function useCreateWorkspace() {
  const [createTrigger] = useCreateWorkspaceMutation();
  return {
    mutate: async (values: CreateWorkspaceValues & { strictJoin?: boolean }) => {
      try {
        const result = await createTrigger(values).unwrap();
        toast.success("Workspace created successfully");
        return result;
      } catch (error: any) {
        toast.error(error.message || "Failed to create workspace");
        throw error;
      }
    },
  };
}

export function useWorkspaces(filters: any) {
  const [cursor, setCursor] = React.useState<string | null>(null);

  const [prevFilters, setPrevFilters] = React.useState(filters);
  if (JSON.stringify(prevFilters) !== JSON.stringify(filters)) {
    setPrevFilters(filters);
    setCursor(null);
  }

  const { data, isLoading, isFetching } = useGetWorkspacesQuery({ ...filters, cursor });

  const fetchNextPage = React.useCallback(() => {
    if (data?.nextCursor) {
      setCursor(data.nextCursor);
    }
  }, [data]);

  return {
    data: data ? { pages: [data] } : undefined,
    isLoading,
    isFetchingNextPage: isFetching && cursor !== null,
    hasNextPage: !!data?.nextCursor,
    fetchNextPage,
  };
}

export function useSetWorkspacePin() {
  const [pinTrigger] = useSetWorkspacePinMutation();
  return {
    mutate: async (payload: { workspaceId: string; isPinned: boolean }) => {
      try {
        await pinTrigger(payload).unwrap();
        toast.success("Workspace pin updated!");
      } catch (error: any) {
        toast.error(error.message || "Failed to update pin");
      }
    },
  };
}

export function useJoinWorkspaceByCode() {
  const [joinTrigger] = useJoinWorkspaceMutation();
  return {
    mutate: async (joinCode: string) => {
      try {
        const data = await joinTrigger(joinCode).unwrap();
        if (data.membershipStatus === "Pending") {
          toast.info("Join request sent. Waiting for approval.");
        } else {
          toast.success("Joined workspace successfully");
        }
        return data;
      } catch (error: any) {
        toast.error(error.message || "Failed to join workspace");
        throw error;
      }
    },
  };
}

export function useWorkspaceHome() {
  const search = useSearch({ from: "/" }) as any;
  const navigate = useNavigate();
  const { refetch } = useGetWorkspacesQuery({ ...search });

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

  const { mutate: createInternal } = useCreateWorkspace();
  const { mutate: setWorkspacePin } = useSetWorkspacePin();

  const [isCreateModalOpen, setIsCreateModalOpen] = React.useState(false);
  const [isJoinModalOpen, setIsJoinModalOpen] = React.useState(false);

  React.useEffect(() => {
    signalRService.startConnection();

    const onUpdate = () => {
      refetch();
    };

    signalRService.on("EntitiesUpdated", onUpdate);
    signalRService.on("EntitiesDeleted", onUpdate);

    return () => {
      signalRService.off("EntitiesUpdated", onUpdate);
      signalRService.off("EntitiesDeleted", onUpdate);
    };
  }, [refetch]);

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
    isCreating: false,
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

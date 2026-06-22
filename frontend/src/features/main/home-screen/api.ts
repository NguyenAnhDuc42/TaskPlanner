import { z } from "zod";
import type { createWorkspaceSchema } from "./type";
import type { WorkspaceSnippetRecord } from "@/types/workspace";
import { workspaceApi } from "@/store/workspaceApi";
import { workspaceSlice, workspaceSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { toast } from "sonner";
import type { PagedResult } from "@/types/paged-result";
import { useNavigate, useSearch } from "@tanstack/react-router";
import React from "react";
import { signalRService } from "@/lib/signalr-service";

type CreateWorkspaceValues = z.infer<typeof createWorkspaceSchema>;

export interface WorkspaceFilters {
  name?: string;
  owned?: boolean;
  isArchived?: boolean;
  variant?: string;
  direction?: "Ascending" | "Descending";
}

export const homeWorkspaceApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkspaces: build.query<PagedResult<WorkspaceSnippetRecord>, WorkspaceFilters & { cursor?: string | null; }>({
      query: (params) => ({ url: "/workspaces", method: "GET", params }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => {
        const filters = { ...queryArgs };
        delete filters.cursor;
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
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(workspaceSlice.actions.upsertMany(data.items));
        } catch { /* ignore */ }
      },
      providesTags: ["Workspaces"],
    }),
    createWorkspace: build.mutation<WorkspaceSnippetRecord, CreateWorkspaceValues & { strictJoin?: boolean }>({
      query: (values) => ({ url: "/workspaces", method: "POST", data: values }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(workspaceSlice.actions.upsert(data));
        } catch { /* ignore */ }
      },
    }),
    setWorkspacePin: build.mutation<boolean, { workspaceId: string; isPinned: boolean }>({
      query: (payload) => ({ url: `/workspaces/${payload.workspaceId}/pin`, method: "PUT", data: { isPinned: payload.isPinned } }),
      async onQueryStarted({ workspaceId, isPinned }, { dispatch, queryFulfilled, getState }) {
        const prev = workspaceSelectors.selectById(getState() as RootState, workspaceId);
        dispatch(workspaceSlice.actions.upsert({ id: workspaceId, isPinned }));
        try {
          await queryFulfilled;
        } catch {
          if (prev) dispatch(workspaceSlice.actions.upsert(prev));
        }
      },
    }),
    joinWorkspace: build.mutation<{
      workspaceId: string;
      membershipStatus: string;
      isNewMember: boolean;
    }, string>({
      query: (joinCode) => ({ url: "/workspaces/join", method: "POST", data: { joinCode } }),
      invalidatesTags: ["Workspaces"],
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
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to create workspace");
        throw error;
      }
    },
  };
}

export function useWorkspaces(filters: WorkspaceFilters) {
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
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to update pin");
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
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to join workspace");
        throw error;
      }
    },
  };
}

export function useWorkspaceHome() {
  const search = useSearch({ from: "/" }) as WorkspaceFilters;
  const navigate = useNavigate({ from: "/" });
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
    (data: Omit<CreateWorkspaceValues, "theme">) => {
      createInternal({ ...data, theme: "Dark" });
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
      navigate({
        search: (prev: WorkspaceFilters) => ({ ...prev, name: name || undefined }),
        replace: true,
      });
    },
    [navigate],
  );

  const handleFilterChange = React.useCallback(
    (newFilters: Partial<WorkspaceFilters>) => {
      navigate({
        search: (prev: WorkspaceFilters) => ({ ...prev, ...newFilters }),
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

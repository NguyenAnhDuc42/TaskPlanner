import { workspaceApi } from "@/store/workspaceApi";
import { memberSlice, statusSlice, statusSelectors, spaceSlice, folderSlice, taskSlice, spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { PagedResult } from "@/types/paged-result";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { WorkflowRecord } from "@/types/projects";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import { EntityLayerType } from "@/types/entity-layer-type";
import { toast } from "sonner";

import { RowAction } from "@/types/row-action";

export interface StatusUpdatePayload {
  id: string | null;
  name: string;
  color: string;
  category: string;
  previousOrderKey: string | null;
  nextOrderKey: string | null;
  action: RowAction;
}

export const workspaceFeatureApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkspaceDetail: build.query<WorkspaceRecord, string>({
      query: (workspaceId) => ({
        url: `/workspaces/${workspaceId}/me/permissions`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Spaces" as const, id }],
    }),

    getWorkspaceMembers: build.query<PagedResult<MemberRecord>, string>({
      query: (workspaceId) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "GET",
        params: { pageSize: 1000 },
      }),
      providesTags: ["Members"],
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(memberSlice.actions.upsertMany(data.items));
        } catch { /* ignore */ }
      },
    }),

    getWorkspaceWorkflows: build.query<WorkflowRecord[], string>({
      query: (workspaceId) => ({
        url: `/workflows`,
        method: "GET",
        params: { workspaceId },
      }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const statuses: Status[] = [];
          data.forEach((wf) => {
            wf.statuses?.forEach((status) => {
              statuses.push(status);
            });
          });
          dispatch(statusSlice.actions.upsertMany(statuses));
        } catch { /* ignore */ }
      },
    }),

    updateWorkflowStatuses: build.mutation<void, { workflowId: string; workspaceId?: string; statuses: StatusUpdatePayload[]; optimisticStatuses?: Status[] }>({
      query: ({ workflowId, statuses }) => ({
        url: `/statuses/workflow/${workflowId}`,
        method: "PUT",
        data: statuses,
      }),
      invalidatesTags: ["Tasks", "Folders", "Spaces"],
      async onQueryStarted({ workflowId, workspaceId, statuses, optimisticStatuses }, { dispatch, queryFulfilled, getState }) {
        let patchResult: { undo: () => void } | undefined;
        const state = getState() as RootState;

        const originalStatuses: Status[] = [];
        if (workspaceId) {
          const workspaceWorkflows = workspaceFeatureApi.endpoints.getWorkspaceWorkflows.select(workspaceId)(state)?.data;
          const wf = workspaceWorkflows?.find(w => w.id === workflowId);
          if (wf?.statuses) {
            originalStatuses.push(...wf.statuses);
          } else {
            const allStatuses = statusSelectors.selectAll(state);
            const wfStatuses = allStatuses.filter(s => s.workflowId === workflowId);
            originalStatuses.push(...wfStatuses);
          }
        }

        if (workspaceId && optimisticStatuses) {
          patchResult = dispatch(
            workspaceFeatureApi.util.updateQueryData("getWorkspaceWorkflows", workspaceId, (draft) => {
              const wf = draft.find(w => w.id === workflowId);
              if (wf) {
                wf.statuses = optimisticStatuses;
              }
            })
          );
          dispatch(statusSlice.actions.upsertMany(optimisticStatuses));

          const deletedIds = statuses.reduce<string[]>((acc, s) => {
            if (s.action === RowAction.Delete && s.id != null) {
              acc.push(s.id);
            }
            return acc;
          }, []);
          if (deletedIds.length > 0) {
            dispatch(statusSlice.actions.removeMany(deletedIds));
          }
        }

        try {
          await queryFulfilled;
        } catch {
          if (patchResult) {
            patchResult.undo();
          }
          if (originalStatuses.length > 0) {
            dispatch(statusSlice.actions.upsertMany(originalStatuses));
          }
          if (optimisticStatuses) {
            const originalIds = new Set(originalStatuses.map(s => s.id));
            const newlyCreatedIds = optimisticStatuses
              .filter(s => !originalIds.has(s.id))
              .map(s => s.id as string);
            if (newlyCreatedIds.length > 0) {
              dispatch(statusSlice.actions.removeMany(newlyCreatedIds));
            }
          }
          toast.error("Failed to update workflow statuses. Your changes have been reverted.");
        }
      }
    }),
    toggleFavorite: build.mutation<{ isFavorite: boolean; favoriteOrderKey: string | null }, { workspaceId: string; entityId: string; entityLayerType: EntityLayerType }>({
      query: ({ workspaceId, entityId, entityLayerType }) => ({
        url: `/workspaces/${workspaceId}/favorites/toggle`,
        method: "POST",
        data: { entityId, entityLayerType },
      }),
      async onQueryStarted({ entityId, entityLayerType }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;

        // Snapshot for rollback
        const prevSpace  = entityLayerType === EntityLayerType.ProjectSpace  ? spaceSelectors.selectById(state, entityId)  : undefined;
        const prevFolder = entityLayerType === EntityLayerType.ProjectFolder ? folderSelectors.selectById(state, entityId) : undefined;
        const prevTask   = entityLayerType === EntityLayerType.ProjectTask   ? taskSelectors.selectById(state, entityId)   : undefined;

        // Optimistic flip for instant UI feedback
        if (entityLayerType === EntityLayerType.ProjectSpace && prevSpace)
          dispatch(spaceSlice.actions.upsert({ id: entityId, isFavorite: !prevSpace.isFavorite }));
        else if (entityLayerType === EntityLayerType.ProjectFolder && prevFolder)
          dispatch(folderSlice.actions.upsert({ id: entityId, isFavorite: !prevFolder.isFavorite }));
        else if (entityLayerType === EntityLayerType.ProjectTask && prevTask)
          dispatch(taskSlice.actions.upsert({ id: entityId, isFavorite: !prevTask.isFavorite }));

        try {
          const { data } = await queryFulfilled;
          // Confirm with server's authoritative isFavorite + favoriteOrderKey
          if (entityLayerType === EntityLayerType.ProjectSpace)
            dispatch(spaceSlice.actions.upsert({ id: entityId, isFavorite: data.isFavorite, favoriteOrderKey: data.favoriteOrderKey ?? undefined }));
          else if (entityLayerType === EntityLayerType.ProjectFolder)
            dispatch(folderSlice.actions.upsert({ id: entityId, isFavorite: data.isFavorite, favoriteOrderKey: data.favoriteOrderKey ?? undefined }));
          else if (entityLayerType === EntityLayerType.ProjectTask)
            dispatch(taskSlice.actions.upsert({ id: entityId, isFavorite: data.isFavorite, favoriteOrderKey: data.favoriteOrderKey ?? undefined }));
        } catch {
          // Rollback to pre-toggle snapshot
          if (entityLayerType === EntityLayerType.ProjectSpace && prevSpace)
            dispatch(spaceSlice.actions.upsert({ id: entityId, isFavorite: prevSpace.isFavorite, favoriteOrderKey: prevSpace.favoriteOrderKey }));
          else if (entityLayerType === EntityLayerType.ProjectFolder && prevFolder)
            dispatch(folderSlice.actions.upsert({ id: entityId, isFavorite: prevFolder.isFavorite, favoriteOrderKey: prevFolder.favoriteOrderKey }));
          else if (entityLayerType === EntityLayerType.ProjectTask && prevTask)
            dispatch(taskSlice.actions.upsert({ id: entityId, isFavorite: prevTask.isFavorite, favoriteOrderKey: prevTask.favoriteOrderKey }));
          toast.error("Failed to update favorite.");
        }
      },
    }),

    getFavorites: build.query<{ spaces: SpaceRecord[]; folders: FolderRecord[]; tasks: TaskRecord[]; nextCursor: string | null; hasNextPage: boolean }, { workspaceId: string; cursor: string | null }>({
      query: ({ workspaceId, cursor }) => ({
        url: `/workspaces/${workspaceId}/favorites`,
        method: "GET",
        params: { cursor, pageSize: 10 },
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.workspaceId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        // Append paged items into the cache list (for cursor tracking)
        currentCache.spaces  = [...currentCache.spaces,  ...newItems.spaces];
        currentCache.folders = [...currentCache.folders, ...newItems.folders];
        currentCache.tasks   = [...currentCache.tasks,   ...newItems.tasks];
        currentCache.nextCursor  = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg?.cursor !== previousArg?.cursor;
      },
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          if (data.spaces.length)  dispatch(spaceSlice.actions.upsertMany(data.spaces));
          if (data.folders.length) dispatch(folderSlice.actions.upsertMany(data.folders));
          if (data.tasks.length)   dispatch(taskSlice.actions.upsertMany(data.tasks));
        } catch { /* ignore */ }
      },
    }),
  }),
});

export const {
  useGetWorkspaceDetailQuery,
  useGetWorkspaceMembersQuery,
  useGetWorkspaceWorkflowsQuery,
  useUpdateWorkflowStatusesMutation,
  useGetFavoritesQuery,
  useToggleFavoriteMutation,
} = workspaceFeatureApi;

export function useUpdateWorkflowStatuses() {
  const [trigger, result] = useUpdateWorkflowStatusesMutation();
  return {
    mutate: (args: { workflowId: string; workspaceId?: string; statuses: StatusUpdatePayload[]; optimisticStatuses?: Status[] }) => trigger(args),
    ...result,
  };
}

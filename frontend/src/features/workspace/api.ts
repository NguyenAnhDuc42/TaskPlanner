import { workspaceApi } from "@/store/workspaceApi";
import { memberSlice, statusSlice, statusSelectors, favoriteSlice, spaceSlice, folderSlice, taskSlice } from "@/store/entityStore";
import type { RootState } from "@/store";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { PagedResult } from "@/types/paged-result";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { WorkflowRecord } from "@/types/projects";
import type { FavoriteRecord } from "@/types/projects/favorite-record";
import type { Status } from "@/types/status";
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

        // Snapshot original state of ALL statuses in this workspace workflow for rollback
        const originalStatuses: Status[] = [];
        if (workspaceId) {
          const workspaceWorkflows = workspaceFeatureApi.endpoints.getWorkspaceWorkflows.select(workspaceId)(state)?.data;
          const wf = workspaceWorkflows?.find(w => w.id === workflowId);
          if (wf?.statuses) {
            originalStatuses.push(...wf.statuses);
          } else {
            // Backup from status selectors if query cache state isn't populated
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
    toggleFavorite: build.mutation<void, { workspaceId: string; entityId: string; entityLayerType: string }>({
      query: ({ workspaceId, entityId, entityLayerType }) => ({
        url: `/workspaces/${workspaceId}/favorites/toggle`,
        method: "POST",
        data: { entityId, entityLayerType },
      }),
      async onQueryStarted({ entityId, entityLayerType }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        let oldIsFavorite: boolean | undefined;

        if (entityLayerType === 'ProjectSpace') {
          const space = state.spaces.entities[entityId];
          if (space) {
            oldIsFavorite = space.isFavorite;
            dispatch(spaceSlice.actions.upsert({ id: entityId, isFavorite: !oldIsFavorite }));
          }
        } else if (entityLayerType === 'ProjectFolder') {
          const folder = state.folders.entities[entityId];
          if (folder) {
            oldIsFavorite = folder.isFavorite;
            dispatch(folderSlice.actions.upsert({ id: entityId, isFavorite: !oldIsFavorite }));
          }
        } else if (entityLayerType === 'ProjectTask') {
          const task = state.tasks.entities[entityId];
          if (task) {
            oldIsFavorite = task.isFavorite;
            dispatch(taskSlice.actions.upsert({ id: entityId, isFavorite: !oldIsFavorite }));
          }
        }

        try {
          await queryFulfilled;
        } catch {
          if (oldIsFavorite !== undefined) {
            if (entityLayerType === 'ProjectSpace') dispatch(spaceSlice.actions.upsert({ id: entityId, isFavorite: oldIsFavorite }));
            else if (entityLayerType === 'ProjectFolder') dispatch(folderSlice.actions.upsert({ id: entityId, isFavorite: oldIsFavorite }));
            else if (entityLayerType === 'ProjectTask') dispatch(taskSlice.actions.upsert({ id: entityId, isFavorite: oldIsFavorite }));
          }
          toast.error("Failed to update favorite status.");
        }
      },
      invalidatesTags: ["Favorites"],
    }),

    getFavorites: build.query<FavoriteRecord[], string>({
      query: (workspaceId) => ({
        url: `/workspaces/${workspaceId}/favorites`,
        method: "GET",
      }),
      providesTags: ["Favorites"],
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(favoriteSlice.actions.upsertMany(data));
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

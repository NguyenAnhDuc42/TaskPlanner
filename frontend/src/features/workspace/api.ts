import { workspaceApi } from "@/store/workspaceApi";
import { memberSlice, statusSlice } from "@/store/entityStore";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { PagedResult } from "@/types/paged-result";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { WorkflowRecord } from "@/types/projects";
import type { Status } from "@/types/status";

export const RowAction = { Create: "Create", Update: "Update", Delete: "Delete" } as const;
export type RowAction = (typeof RowAction)[keyof typeof RowAction];

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
      providesTags: (result, error, id) => [{ type: "Spaces" as const, id }],
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
        } catch {}
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
        } catch {}
      },
    }),

    updateWorkflowStatuses: build.mutation<void, { workflowId: string; workspaceId?: string; statuses: StatusUpdatePayload[]; optimisticStatuses?: Status[] }>({
      query: ({ workflowId, statuses }) => ({
        url: `/statuses/workflow/${workflowId}`,
        method: "PUT",
        data: statuses,
      }),
      invalidatesTags: ["Tasks", "Folders", "Spaces"],
      async onQueryStarted({ workflowId, workspaceId, statuses, optimisticStatuses }, { dispatch, queryFulfilled }) {
        let patchResult: any;

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

          const deletedIds = statuses
            .filter((s) => s.action === RowAction.Delete && s.id != null)
            .map((s) => s.id as string);
          if (deletedIds.length > 0) {
            dispatch(statusSlice.actions.removeMany(deletedIds));
          }
        }

        try {
          const { queryClient } = await import("@/lib/query-client");
          await queryFulfilled;
          // Invalidate TanStack Query to refresh legacy detail tabs in sync
          queryClient.invalidateQueries();
        } catch {
          if (patchResult) {
            patchResult.undo();
          }
        }
      }
    }),
  }),
});

export const {
  useGetWorkspaceDetailQuery,
  useGetWorkspaceMembersQuery,
  useGetWorkspaceWorkflowsQuery,
  useUpdateWorkflowStatusesMutation,
} = workspaceFeatureApi;

// Backwards-compatible hook wrapper for status updates to avoid breaking current forms
export function useUpdateWorkflowStatuses() {
  const [trigger, result] = useUpdateWorkflowStatusesMutation();
  return {
    mutate: (args: { workflowId: string; workspaceId?: string; statuses: StatusUpdatePayload[]; optimisticStatuses?: Status[] }) => trigger(args),
    ...result,
  };
}

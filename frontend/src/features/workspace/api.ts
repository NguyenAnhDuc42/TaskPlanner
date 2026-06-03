import { workspaceApi } from "@/store/workspaceApi";
import { memberSlice, statusSlice, statusSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
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
      async onQueryStarted({ workflowId, workspaceId, statuses, optimisticStatuses }, { dispatch, queryFulfilled, getState }) {
        let patchResult: any;
        const state = getState() as RootState;

        // Snapshot original state of ALL statuses in this workspace workflow for rollback
        const originalStatuses: Status[] = [];
        if (workspaceId) {
          const workspaceWorkflows = (state as any).workspaceApi?.queries?.[`getWorkspaceWorkflows("${workspaceId}")`]?.data as WorkflowRecord[] | undefined;
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
          const { queryClient } = await import("@/lib/query-client");
          await queryFulfilled;
          // Invalidate TanStack Query to refresh legacy detail tabs in sync
          queryClient.invalidateQueries();
        } catch {
          if (patchResult) {
            patchResult.undo();
          }
          // Rollback Entity Store status slice changes
          if (originalStatuses.length > 0) {
            dispatch(statusSlice.actions.upsertMany(originalStatuses));
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

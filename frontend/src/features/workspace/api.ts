import { workspaceApi } from "@/store/workspaceApi";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";

// getWorkspaceMembers/getWorkspaceStatuses/updateWorkflowStatuses/getFavorites used to live here —
// removed (dead: zero callers anywhere in the app) once Member/Status/Favorite were fully migrated
// to the sync engine (MemberMutations/StatusMutations/FavoriteMutations + rootStore, see
// BACKEND_SYNC_CONTEXT.md §11a). getWorkspaceDetail stays — it's the per-workspace permissions
// fetch, a separate concern from the synced entity data and not part of Bootstrap's payload.
export const workspaceFeatureApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkspaceDetail: build.query<WorkspaceRecord, string>({
      query: (workspaceId) => ({
        url: `/workspaces/${workspaceId}/me/permissions`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Workspaces" as const, id }],
    }),
  }),
});

export const { useGetWorkspaceDetailQuery } = workspaceFeatureApi;

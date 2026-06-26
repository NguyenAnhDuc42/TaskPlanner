import { createFileRoute } from "@tanstack/react-router";
import MembersIndex from "@/features/workspace/contents/members/members-index";
import { store } from "@/store";
import { membersApi } from "@/features/workspace/contents/members/members-api";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute("/workspaces/$workspaceId/members")({
  loader: async ({ params: { workspaceId } }) => {
    try {
      await store.dispatch(
        membersApi.endpoints.getMembers.initiate({ workspaceId, cursor: null })
      ).unwrap();
    } catch {
      // Non-fatal — component renders with empty/stale state
    }
  },
  pendingComponent: ViewSkeleton,
  component: MembersIndex,
});

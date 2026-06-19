import { createFileRoute, redirect, useParams } from "@tanstack/react-router";
import { SpaceView } from "@/features/workspace/contents/views/space/space-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { spaceApi } from "@/features/workspace/contents/views/space/space-api";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: async ({ params: { workspaceId, spaceId } }) => {
    try {
      const [detail, items, access] = await Promise.all([
        store.dispatch(spaceApi.endpoints.getSpaceDetail.initiate(spaceId)).unwrap(),
        store.dispatch(spaceApi.endpoints.getSpaceItems.initiate(spaceId)).unwrap(),
        store.dispatch(spaceApi.endpoints.getEntityAccess.initiate(spaceId)).unwrap(),
      ]);
      return { detail, items, access };
    } catch {
      throw redirect({ to: "/workspaces/$workspaceId", params: { workspaceId } });
    }
  },
  component: SpaceContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function SpaceContent() {
  const { spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  return <SpaceView spaceId={spaceId} />;
}

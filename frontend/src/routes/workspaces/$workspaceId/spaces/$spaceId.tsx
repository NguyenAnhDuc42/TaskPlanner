import { createFileRoute, useParams } from "@tanstack/react-router";
import { SpaceView } from "@/features/workspace/contents/layer-detail/views/space/space-view";
import { spaceQueryOptions } from "@/features/workspace/contents/layer-detail/views/space/space-api";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: ({ context: { queryClient }, params: { workspaceId, spaceId } }) => {
    queryClient.ensureQueryData(spaceQueryOptions.detail(workspaceId, spaceId));
    queryClient.ensureQueryData(spaceQueryOptions.items(spaceId));
  },
  component: SpaceContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function SpaceContent() {
  const { workspaceId, spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  return <SpaceView workspaceId={workspaceId} spaceId={spaceId} />;
}

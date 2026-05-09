import { createFileRoute, useParams } from "@tanstack/react-router";
import { SpaceView } from "@/features/workspace/contents/layer-detail/views/space/space-view";
import { spaceQueryOptions } from "@/features/workspace/contents/layer-detail/views/space/space-api";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: ({ context: { queryClient }, params: { workspaceId, spaceId } }) => {
    queryClient.ensureQueryData(spaceQueryOptions.detail(workspaceId, spaceId));
  },
  component: SpaceContent,
});

function SpaceContent() {
  const { workspaceId, spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  return <SpaceView workspaceId={workspaceId} spaceId={spaceId} />;
}

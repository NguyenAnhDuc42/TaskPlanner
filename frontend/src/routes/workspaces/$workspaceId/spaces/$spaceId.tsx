import { createFileRoute, useParams } from "@tanstack/react-router";
import { SpaceView } from "@/features/workspace/contents/views/space/space-view";
import { NotFoundScreen } from "@/components/not-found-screen";

// Space/Folder/Task/Status are fully Bootstrap+Delta covered via MobX by the time this route
// renders — no loader/prefetch needed (SpaceView gates on useSyncReady() itself).
export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  component: SpaceContent,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function SpaceContent() {
  const { spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  return <SpaceView key={spaceId} spaceId={spaceId} />;
}

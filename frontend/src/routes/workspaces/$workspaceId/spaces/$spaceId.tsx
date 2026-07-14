import { createFileRoute, useParams } from "@tanstack/react-router";
import { useEffect } from "react";
import { SpaceView } from "@/features/workspace/contents/views/space/space-view";
import { NotFoundScreen } from "@/components/not-found-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  component: SpaceContent,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function SpaceContent() {
  const { workspaceId, spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  useEffect(() => {
    localStorage.setItem(`lastSpaceId:${workspaceId}`, spaceId);
  }, [workspaceId, spaceId]);

  return <SpaceView spaceId={spaceId} />;
}

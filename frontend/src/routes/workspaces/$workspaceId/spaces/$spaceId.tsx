import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/spaces/$spaceId",
)({
  component: SpaceContent,
});

function SpaceContent() {
  return <ViewContainer layerType="ProjectSpace" />;
}

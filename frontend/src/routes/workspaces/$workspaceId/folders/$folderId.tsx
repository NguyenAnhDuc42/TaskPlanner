import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  component: FolderContent,
});

function FolderContent() {
  return <ViewContainer layerType="ProjectFolder" />;
}

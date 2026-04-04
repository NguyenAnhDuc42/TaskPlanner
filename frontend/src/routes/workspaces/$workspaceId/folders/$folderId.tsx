import { createFileRoute } from "@tanstack/react-router";
import { HierarchyLayerIndex } from "@/features/workspace/contents/hierarchy/hierarchy-layer-index";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  component: FolderContent,
});

function FolderContent() {
  return <HierarchyLayerIndex />;
}

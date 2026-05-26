import { createFileRoute } from "@tanstack/react-router";
import { FolderView } from "@/features/workspace/contents/views/folder/folder-view";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  component: FolderContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function FolderContent() {
  return <FolderView />;
}

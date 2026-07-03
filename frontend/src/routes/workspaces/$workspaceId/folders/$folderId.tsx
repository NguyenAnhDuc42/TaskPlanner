import { createFileRoute, useParams } from "@tanstack/react-router";
import { FolderView } from "@/features/workspace/contents/views/folder/folder-view";
import { NotFoundScreen } from "@/components/not-found-screen";

// Folder/Task/Status are fully Bootstrap+Delta covered via MobX by the time this route
// renders — no loader/prefetch needed (FolderView gates on useSyncReady() itself).
export const Route = createFileRoute("/workspaces/$workspaceId/folders/$folderId")({
  component: FolderContent,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function FolderContent() {
  const { folderId } = useParams({ from: "/workspaces/$workspaceId/folders/$folderId" });
  return <FolderView  folderId={folderId} />;
}

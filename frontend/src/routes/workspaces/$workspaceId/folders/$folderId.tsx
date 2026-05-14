import { createFileRoute, useParams } from "@tanstack/react-router";
import { folderQueryOptions } from "@/features/workspace/contents/layer-detail/views/folder/folder-api";
import { FolderView } from "@/features/workspace/contents/layer-detail/views/folder/folder-view";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: ({ context: { queryClient }, params: { workspaceId, folderId } }) => {
    queryClient.ensureQueryData(folderQueryOptions.detail(workspaceId, folderId));
    queryClient.ensureQueryData(folderQueryOptions.items(folderId));
  },
  component: FolderContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function FolderContent() {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, folderId } = params;
  return <FolderView workspaceId={workspaceId} folderId={folderId} />;
}

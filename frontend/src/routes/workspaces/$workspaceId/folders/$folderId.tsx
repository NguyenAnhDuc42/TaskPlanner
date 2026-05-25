import { createFileRoute, useParams } from "@tanstack/react-router";
import { folderQueryOptions } from "@/features/workspace/contents/views/folder/folder-api";
import { FolderView } from "@/features/workspace/contents/views/folder/folder-view";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: async ({ context: { queryClient }, params: { folderId } }) => {
    queryClient.ensureQueryData(folderQueryOptions.detail(folderId));
    await queryClient.prefetchInfiniteQuery(folderQueryOptions.tasks(folderId));
  },
  component: FolderContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function FolderContent() {
  return <FolderView  />;
}

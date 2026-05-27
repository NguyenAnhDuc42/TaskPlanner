import { createFileRoute } from "@tanstack/react-router";
import { FolderView } from "@/features/workspace/contents/views/folder/folder-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { folderApi } from "@/features/workspace/contents/views/folder/folder-api";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: ({ params: { folderId } }) =>
    Promise.all([
      store.dispatch(folderApi.endpoints.getFolderDetail.initiate(folderId)),
      store.dispatch(
        folderApi.endpoints.getFolderTasks.initiate({ folderId, cursor: null })
      ),
    ]),
  component: FolderContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function FolderContent() {
  return <FolderView />;
}

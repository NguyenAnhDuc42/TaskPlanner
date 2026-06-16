import { createFileRoute, useParams } from "@tanstack/react-router";
import { FolderView } from "@/features/workspace/contents/views/folder/folder-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { folderApi } from "@/features/workspace/contents/views/folder/folder-api";

export const Route = createFileRoute("/workspaces/$workspaceId/folders/$folderId",
)({
  loader: async ({ params: { folderId } }) => {
    const [detail, tasks] = await Promise.all([
      store.dispatch(folderApi.endpoints.getFolderDetail.initiate(folderId)).unwrap(),
      store.dispatch(folderApi.endpoints.getFolderTasks.initiate({ folderId, cursor: null })).unwrap(),
    ]);
    return { detail, tasks };
  },
  component: FolderContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function FolderContent() {
  const { folderId } = useParams({ from: "/workspaces/$workspaceId/folders/$folderId" });
  return <FolderView folderId={folderId} />;
}

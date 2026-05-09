import { createFileRoute, useParams } from "@tanstack/react-router";
import { folderQueryOptions } from "@/features/workspace/contents/layer-detail/views/folder/folder-api";
import { FolderView } from "@/features/workspace/contents/layer-detail/views/folder/folder-view";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: ({ context: { queryClient }, params: { workspaceId, folderId } }) => {
    queryClient.ensureQueryData(folderQueryOptions.detail(workspaceId, folderId));
  },
  component: FolderContent,
});

function FolderContent() {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, folderId } = params;
  return <FolderView workspaceId={workspaceId} folderId={folderId} />;
}

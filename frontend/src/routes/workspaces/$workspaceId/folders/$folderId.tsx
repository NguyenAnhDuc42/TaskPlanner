import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";
import { EntityLayerType } from "@/types/entity-layer-type";

import { workspaceQueryOptions } from "@/features/workspace/api";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: ({ context: { queryClient }, params: { workspaceId, folderId } }) => {
    queryClient.ensureQueryData(workspaceQueryOptions.folderDetail(workspaceId, folderId));
  },
  component: FolderContent,
});

function FolderContent() {
  return <LayerDetailIndex forcedLayerType={EntityLayerType.ProjectFolder} />;
}

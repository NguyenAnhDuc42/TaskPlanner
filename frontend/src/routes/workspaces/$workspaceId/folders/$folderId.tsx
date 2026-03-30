import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";
import { viewsQueryOptions } from "@/features/workspace/contents/views/views-api";
import { EntityLayerType } from "@/types/relationship-type";

export const Route = createFileRoute(
  "/workspaces/$workspaceId/folders/$folderId",
)({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(
      viewsQueryOptions.list(params.folderId, EntityLayerType.ProjectFolder),
    ),
  component: FolderContent,
});

function FolderContent() {
  return <ViewContainer layerType={EntityLayerType.ProjectFolder} />;
}

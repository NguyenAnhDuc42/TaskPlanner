import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";
import { viewsQueryOptions } from "@/features/workspace/contents/views/views-api";
import { EntityLayerType } from "@/types/relationship-type";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(
      viewsQueryOptions.list(params.spaceId, EntityLayerType.ProjectSpace),
    ),
  component: SpaceContent,
});

function SpaceContent() {
  return <ViewContainer layerType={EntityLayerType.ProjectSpace} />;
}

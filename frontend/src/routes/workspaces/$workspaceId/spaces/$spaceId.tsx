import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";
import { EntityLayerType } from "@/types/entity-layer-type";

import { workspaceQueryOptions } from "@/features/workspace/api";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: ({ context: { queryClient }, params: { workspaceId, spaceId } }) => {
    queryClient.ensureQueryData(workspaceQueryOptions.spaceDetail(workspaceId, spaceId));
  },
  component: SpaceContent,
});

function SpaceContent() {
  return <LayerDetailIndex forcedLayerType={EntityLayerType.ProjectSpace} />;
}

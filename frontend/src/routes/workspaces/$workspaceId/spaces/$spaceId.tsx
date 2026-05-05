import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";
import { EntityLayerType } from "@/types/entity-layer-type";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  component: SpaceContent,
});

function SpaceContent() {
  return <LayerDetailIndex forcedLayerType={EntityLayerType.ProjectSpace} />;
}

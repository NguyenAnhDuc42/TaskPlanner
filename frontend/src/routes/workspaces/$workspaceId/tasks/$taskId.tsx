import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";

import { entityQueryOptions } from "@/features/workspace/contents/layer-detail/layer-api";
import { EntityLayerType } from "@/types/entity-layer-type";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  loader: ({ context: { queryClient }, params: { workspaceId, taskId } }) => {
    queryClient.ensureQueryData(entityQueryOptions.detail(workspaceId, taskId, EntityLayerType.ProjectTask));
  },
  component: TaskContent,
});

function TaskContent() {
  return <LayerDetailIndex />;
}

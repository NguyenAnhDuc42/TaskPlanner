import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";

import { workspaceQueryOptions } from "@/features/workspace/api";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  loader: ({ context: { queryClient }, params: { workspaceId, taskId } }) => {
    queryClient.ensureQueryData(workspaceQueryOptions.taskDetail(workspaceId, taskId));
  },
  component: TaskContent,
});

function TaskContent() {
  return <LayerDetailIndex />;
}

import { createFileRoute } from "@tanstack/react-router";
import { LayerDetailIndex } from "@/features/workspace/contents/layer-detail/layer-detail-index";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  component: TaskContent,
});

function TaskContent() {
  return <LayerDetailIndex />;
}

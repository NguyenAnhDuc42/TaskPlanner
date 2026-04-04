import { createFileRoute } from "@tanstack/react-router";
import { HierarchyLayerIndex } from "@/features/workspace/contents/hierarchy/hierarchy-layer-index";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  component: TaskContent,
});

function TaskContent() {
  return <HierarchyLayerIndex />;
}

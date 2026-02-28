import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";

export const Route = createFileRoute("/workspaces/$workspaceId/lists/$listId")({
  component: ListContent,
});

function ListContent() {
  return <ViewContainer layerType="ProjectList" />;
}

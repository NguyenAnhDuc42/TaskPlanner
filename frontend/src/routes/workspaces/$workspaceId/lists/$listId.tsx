import { createFileRoute } from "@tanstack/react-router";
import { ViewContainer } from "@/features/workspace/contents/views/view-container";
import { viewsQueryOptions } from "@/features/workspace/contents/views/views-api";
import { EntityLayerType } from "@/types/relationship-type";

export const Route = createFileRoute("/workspaces/$workspaceId/lists/$listId")({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(
      viewsQueryOptions.list(params.listId, EntityLayerType.ProjectList),
    ),
  component: ListContent,
});

function ListContent() {
  return <ViewContainer layerType={EntityLayerType.ProjectList} />;
}

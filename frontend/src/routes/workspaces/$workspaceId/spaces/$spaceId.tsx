import { createFileRoute, useParams } from "@tanstack/react-router";
import { SpaceView } from "@/features/workspace/contents/views/space/space-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { spaceApi } from "@/features/workspace/contents/views/space/space-api";
import { documentApi } from "@/features/workspace/contents/views/view-components/document-api";
import { NotFoundScreen } from "@/components/not-found-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/spaces/$spaceId")({
  loader: async ({ params: { spaceId } }) => {
    const [detail, items, access] = await Promise.all([
      store.dispatch(spaceApi.endpoints.getSpaceDetail.initiate(spaceId)).unwrap(),
      store.dispatch(spaceApi.endpoints.getSpaceItems.initiate({ spaceId, cursor: null })).unwrap(),
      store.dispatch(spaceApi.endpoints.getEntityAccess.initiate(spaceId)).unwrap(),
    ]);
    if (detail?.defaultDocumentId) {
      await store.dispatch(documentApi.endpoints.getDocumentBlocks.initiate(detail.defaultDocumentId)).unwrap();
    }
    return { detail, items, access };
  },
  component: SpaceContent,
  pendingComponent: ViewSkeleton,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function SpaceContent() {
  const { spaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  return <SpaceView key={spaceId} spaceId={spaceId} />;
}

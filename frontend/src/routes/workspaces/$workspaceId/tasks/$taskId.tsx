import { createFileRoute, useParams } from "@tanstack/react-router";
import { TaskView } from "@/features/workspace/contents/views/task/task-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { taskApi } from "@/features/workspace/contents/views/task/task-api";
import { documentApi } from "@/features/workspace/contents/views/view-components/document-api";
import { NotFoundScreen } from "@/components/not-found-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  loader: async ({ params: { taskId } }) => {
    const detail = await store.dispatch(taskApi.endpoints.getTaskDetail.initiate(taskId)).unwrap();
    if (detail?.[0]?.defaultDocumentId) {
      await store.dispatch(documentApi.endpoints.getDocumentBlocks.initiate(detail[0].defaultDocumentId)).unwrap();
    }
    return { detail };
  },
  component: TaskContent,
  pendingComponent: ViewSkeleton,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function TaskContent() {
  const { taskId } = useParams({ from: "/workspaces/$workspaceId/tasks/$taskId" });
  return <TaskView key={taskId} taskId={taskId} />;
}

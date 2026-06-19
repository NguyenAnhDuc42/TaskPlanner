import { createFileRoute, redirect, useParams } from "@tanstack/react-router";
import { TaskView } from "@/features/workspace/contents/views/task/task-view";
import { ViewSkeleton } from "@/components/view-skeleton";
import { store } from "@/store";
import { taskApi } from "@/features/workspace/contents/views/task/task-api";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  loader: async ({ params: { workspaceId, taskId } }) => {
    try {
      const detail = await store.dispatch(taskApi.endpoints.getTaskDetail.initiate(taskId)).unwrap();
      return { detail };
    } catch {
      throw redirect({ to: "/workspaces/$workspaceId", params: { workspaceId } });
    }
  },
  component: TaskContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function TaskContent() {
  const { taskId } = useParams({ from: "/workspaces/$workspaceId/tasks/$taskId" });
  return <TaskView taskId={taskId} />;
}

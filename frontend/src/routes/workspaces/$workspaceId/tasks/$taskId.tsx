import { createFileRoute, useParams } from "@tanstack/react-router";
import { taskQueryOptions } from "@/features/workspace/contents/layer-detail/views/task/task-api";
import { TaskView } from "@/features/workspace/contents/layer-detail/views/task/task-view";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  loader: ({ context: { queryClient }, params: { workspaceId, taskId } }) => {
    queryClient.ensureQueryData(taskQueryOptions.detail(workspaceId, taskId));
  },
  component: TaskContent,
});

function TaskContent() {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, taskId } = params;
  return <TaskView workspaceId={workspaceId} taskId={taskId} />;
}

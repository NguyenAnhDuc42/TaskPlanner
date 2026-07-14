import { createFileRoute, useParams } from "@tanstack/react-router";
import { TaskView } from "@/features/workspace/contents/views/task/task-view";
import { NotFoundScreen } from "@/components/not-found-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  component: TaskContent,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function TaskContent() {
  const { taskId } = useParams({ from: "/workspaces/$workspaceId/tasks/$taskId" });
  return <TaskView key={taskId} taskId={taskId} />;
}

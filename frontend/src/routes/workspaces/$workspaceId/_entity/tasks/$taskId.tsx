import { createFileRoute, useParams } from "@tanstack/react-router";
import { TaskViewBody } from "@/features/workspace/contents/views/task/task-view";
import { NotFoundScreen } from "@/components/not-found-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/_entity/tasks/$taskId")({
  component: TaskContent,
  errorComponent: () => <NotFoundScreen />,
  pendingMs: 0,
});

function TaskContent() {
  const { taskId } = useParams({ from: "/workspaces/$workspaceId/_entity/tasks/$taskId" });
  return <TaskViewBody key={taskId} taskId={taskId} />;
}

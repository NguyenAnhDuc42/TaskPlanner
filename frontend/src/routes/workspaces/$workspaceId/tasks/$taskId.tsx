import { createFileRoute, useParams } from "@tanstack/react-router";
import { TaskView } from "@/features/workspace/contents/views/task/task-view";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute("/workspaces/$workspaceId/tasks/$taskId")({
  component: TaskContent,
  pendingComponent: ViewSkeleton,
  pendingMs: 0,
});

function TaskContent() {
  const params = useParams({ strict: false }) as any;
  const { taskId } = params;
  return <TaskView taskId={taskId} />;
}

import { createFileRoute } from "@tanstack/react-router";
import { LoadingScreen } from "@/components/loading-screen";
import { MyTasksPage } from "@/features/workspace/contents/my-tasks/my-tasks-page";

export const Route = createFileRoute("/workspaces/$workspaceId/my-tasks")({
  pendingComponent: LoadingScreen,
  component: MyTasksPage,
});

import { createFileRoute } from "@tanstack/react-router";
import { ComingSoon } from "@/components/coming-soon";
import { LoadingScreen } from "@/components/loading-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  pendingComponent: LoadingScreen,
  component: () => (
    <ComingSoon
      title="Command Center"
      description="A keyboard-first power hub for searching, navigating, and running commands across your entire workspace."
    />
  ),
});

import { createFileRoute } from "@tanstack/react-router";
import { ComingSoon } from "@/components/coming-soon";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  pendingComponent: ViewSkeleton,
  component: () => (
    <ComingSoon
      title="Command Center"
      description="A keyboard-first power hub for searching, navigating, and running commands across your entire workspace."
    />
  ),
});

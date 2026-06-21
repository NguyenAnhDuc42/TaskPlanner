import { createFileRoute } from "@tanstack/react-router";
import { ComingSoon } from "@/components/coming-soon";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  component: () => (
    <ComingSoon
      title="Command Center"
      description="A keyboard-first power hub for searching, navigating, and running commands across your entire workspace."
    />
  ),
});

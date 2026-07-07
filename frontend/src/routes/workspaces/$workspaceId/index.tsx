import { createFileRoute } from "@tanstack/react-router";
import { workspaceSearchSchema } from "../workspace-search-schema";
import { ComingSoon } from "@/components/coming-soon";
import { LoadingScreen } from "@/components/loading-screen";

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  pendingComponent: LoadingScreen,
  component: () => (
    <ComingSoon
      title="Command Center"
      description="A keyboard-first power hub for searching, navigating, and running commands across your entire workspace."
    />
  ),
});

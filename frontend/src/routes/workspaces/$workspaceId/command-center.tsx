import { createFileRoute } from "@tanstack/react-router";
import { CommandCenterView } from "@/features/workspace/contents/command-center/command-center-view";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  component: CommandCenterView,
});

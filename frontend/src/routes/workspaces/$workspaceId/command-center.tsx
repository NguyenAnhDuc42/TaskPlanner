import { createFileRoute } from "@tanstack/react-router";
import CommandCenterIndex from "@/features/workspace/contents/command-center/command-center-index";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  component: CommandCenterIndex,
});

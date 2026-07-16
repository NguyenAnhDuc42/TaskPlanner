import { createFileRoute } from "@tanstack/react-router";
import { EntityShell } from "@/features/workspace/contents/views/entity-shell";

export const Route = createFileRoute("/workspaces/$workspaceId/_entity")({
  component: EntityShell,
});

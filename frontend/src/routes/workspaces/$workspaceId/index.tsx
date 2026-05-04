import { createFileRoute } from "@tanstack/react-router";
import { workspaceSearchSchema } from "../$workspaceId";
import CommandCenterIndex from "@/features/workspace/contents/command-center/command-center-index";

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceIndex,
});

function WorkspaceIndex() {
  return <CommandCenterIndex />;
}

import { createFileRoute } from "@tanstack/react-router";
import {
  WorkspaceProvider,
  useWorkspace,
} from "@/features/workspace/context/workspace-provider";
import { WorkspaceSessionProvider } from "@/features/workspace/context/workspace-session-provider";
import { WorkspaceLayout } from "@/features/workspace/components/workspace-layout";
import { z } from "zod";
import { Loader2 } from "lucide-react";

export const workspaceSearchSchema = z.object({});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  component: WorkspaceRoot,
});

function WorkspaceRoot() {
  const { workspaceId } = Route.useParams();

  return (
    <WorkspaceProvider workspaceId={workspaceId}>
      <WorkspaceContent />
    </WorkspaceProvider>
  );
}

function WorkspaceContent() {
  const { isLoading, workspaceId } = useWorkspace();

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background text-primary">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <WorkspaceSessionProvider workspaceId={workspaceId}>
      <WorkspaceLayout />
    </WorkspaceSessionProvider>
  );
}

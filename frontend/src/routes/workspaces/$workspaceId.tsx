import { createFileRoute } from "@tanstack/react-router";
import {
  WorkspaceProvider,
  useWorkspace,
} from "@/features/workspace/context/workspace-provider";
import { WorkspaceLayout } from "@/features/workspace/components/workspace-layout";
import { z } from "zod";
import { Loader2 } from "lucide-react";

import { workspaceQueryOptions } from "@/features/workspace/api";

export const workspaceSearchSchema = z.object({});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  parseParams: (params) => ({
    workspaceId: z.uuid().parse(params.workspaceId),
  }),
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  loader: ({ context: { queryClient }, params: { workspaceId } }) => {
    queryClient.ensureQueryData(workspaceQueryOptions.workflows(workspaceId));
    queryClient.ensureQueryData(workspaceQueryOptions.members(workspaceId));
    queryClient.ensureQueryData(workspaceQueryOptions.detail(workspaceId));
  },
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
  const { isLoading } = useWorkspace();

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background text-primary">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return <WorkspaceLayout />;
}

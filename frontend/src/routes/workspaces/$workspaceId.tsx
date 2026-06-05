import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  WorkspaceProvider,
  useWorkspace,
} from "@/features/workspace/context/workspace-provider";
import { WorkspaceLayout } from "@/features/workspace/components/workspace-layout";
import { z } from "zod";
import { Loader2 } from "lucide-react";
import { store } from "@/store";
import { workspaceFeatureApi } from "@/features/workspace/api";
import { getCookie } from "@/lib/cookie-utils";

export const workspaceSearchSchema = z.object({});

export const Route = createFileRoute("/workspaces/$workspaceId")({
  beforeLoad: async () => {
    const isLoggedIn = !!getCookie("is_logged_in");
    if (!isLoggedIn) {
      throw redirect({ to: "/auth/sign-in" });
    }
  },
  parseParams: (params) => ({
    workspaceId: z.uuid().parse(params.workspaceId),
  }),
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  loader: ({ params: { workspaceId } }) => {
    return Promise.all([
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceDetail.initiate(workspaceId)),
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceMembers.initiate(workspaceId)),
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceWorkflows.initiate(workspaceId)),
    ]);
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

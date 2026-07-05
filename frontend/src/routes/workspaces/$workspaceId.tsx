import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { WorkspaceProvider } from "@/features/workspace/context/workspace-provider";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { WorkspaceLayout } from "@/features/workspace/components/workspace-layout";
import { SyncProvider } from "@/sync/sync-provider";
import { z } from "zod";
import { Loader2, ShieldAlert, ArrowLeft } from "lucide-react";
import axios from "axios";
import { Button } from "@/components/ui/button";
import { getCookie } from "@/lib/cookie-utils";

import { workspaceSearchSchema } from "./workspace-search-schema";
import { ViewSkeleton } from "@/components/view-skeleton";

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
    sessionStorage.setItem("activeWorkspaceId", workspaceId);
  },
  pendingComponent:ViewSkeleton,
  component: WorkspaceRoot,
});

function WorkspaceRoot() {
  const { workspaceId } = Route.useParams();

  return (
    <SyncProvider workspaceId={workspaceId}>
      <WorkspaceProvider workspaceId={workspaceId}>
        <WorkspaceContent />
      </WorkspaceProvider>
    </SyncProvider>
  );
}

function WorkspaceContent() {
  const { isLoading, isError, error } = useWorkspace();
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background text-primary">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (isError) {
    const status = axios.isAxiosError(error) ? error.response?.status : undefined;
    const isPermissionDenied = status === 403 || status === 404;
    return (
      <div className="h-screen w-full flex flex-col items-center justify-center gap-3 bg-background text-center px-6">
        <ShieldAlert className="h-10 w-10 text-destructive/70" />
        <h2 className="text-base font-bold text-foreground">
          {isPermissionDenied ? "You don't have access to this workspace" : "Something went wrong"}
        </h2>
        <p className="text-sm text-muted-foreground max-w-xs">
          {isPermissionDenied
            ? "You're no longer a member of this workspace, or your access has been suspended."
            : "We couldn't load this workspace. Please try again."}
        </p>
        <Button variant="outline" className="gap-1.5 mt-1" onClick={() => navigate({ to: "/" })}>
          <ArrowLeft className="h-4 w-4" />
          Go back
        </Button>
      </div>
    );
  }

  return <WorkspaceLayout />;
}

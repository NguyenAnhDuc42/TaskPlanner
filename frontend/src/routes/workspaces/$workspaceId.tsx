import { createFileRoute, redirect } from "@tanstack/react-router";
import { WorkspaceProvider } from "@/features/workspace/context/workspace-provider";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { WorkspaceLayout } from "@/features/workspace/components/workspace-layout";
import { SyncProvider } from "@/sync/sync-provider";
import { z } from "zod";
import { Loader2 } from "lucide-react";
import { store } from "@/store";
import { workspaceFeatureApi } from "@/features/workspace/api";
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
    // Store before API calls so the axios interceptor can use it when URL hasn't updated yet
    sessionStorage.setItem("activeWorkspaceId", workspaceId);
    store.dispatch(workspaceFeatureApi.endpoints.getFavorites.initiate({ workspaceId, cursor: null }));
    return Promise.all([
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceDetail.initiate(workspaceId)),
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceMembers.initiate(workspaceId)),
      store.dispatch(workspaceFeatureApi.endpoints.getWorkspaceStatuses.initiate()),
    ]);
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

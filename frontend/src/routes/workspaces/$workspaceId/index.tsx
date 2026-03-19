import { createFileRoute } from "@tanstack/react-router";
import { DashboardIndex } from "@/features/workspace/contents/dashboard/dashboard-index";
import { dashboardQueryOptions } from "@/features/workspace/contents/dashboard/dashboard-api";
import { EntityLayerType } from "@/types/relationship-type";
import { workspaceSearchSchema } from "../$workspaceId";

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  loaderDeps: ({ search: { dashboardId } }) => ({ dashboardId }),
  loader: async ({ context, params, deps }) => {
    // 1. Pre-fetch the list of dashboards for this workspace
    await context.queryClient.ensureQueryData(
      dashboardQueryOptions.list(params.workspaceId, EntityLayerType.ProjectWorkspace)
    );

    // 2. If a dashboard is selected, pre-fetch its widgets
    if (deps.dashboardId) {
      await context.queryClient.ensureQueryData(
        dashboardQueryOptions.widgets(deps.dashboardId)
      );
    }
  },
  component: DashboardIndex,
});

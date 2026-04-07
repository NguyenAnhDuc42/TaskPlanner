import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useDashboards } from "./dashboard-api";
import { Loader2, Plus } from "lucide-react";
import { Route } from "@/routes/workspaces/$workspaceId";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useState } from "react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { DashboardItem } from "./dashboard-components/dashboard-item";
import { CreateDashboardForm } from "./dashboard-components/create-dashboard-form";

export function DashboardSidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: dashboards, isLoading } = useDashboards(
    workspaceId || "",
    EntityLayerType.ProjectWorkspace,
  );
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const { dashboardId } = Route.useSearch();
  const navigate = Route.useNavigate();

  useEffect(() => {
    if (!dashboardId && dashboards?.items && dashboards.items.length > 0) {
      navigate({
        search: (prev: any) => ({
          ...prev,
          dashboardId: dashboards.items[0].id,
        }),
        replace: true,
      });
    }
  }, [dashboardId, dashboards, navigate]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-4">
        <Loader2 className="h-4 w-4 animate-spin mr-2" />
        <span className="text-xs text-muted-foreground">Loading...</span>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col gap-4">
      <div className="flex items-center justify-between px-3">
        <span className="text-[10px] font-bold text-[var(--theme-text-normal)] uppercase tracking-[0.2em] opacity-80">
          Dashboards
        </span>
        <DialogFormWrapper
          title="Create Dashboard"
          open={isCreateOpen}
          onOpenChange={setIsCreateOpen}
          trigger={
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] rounded-md transition-all"
            >
              <Plus className="h-4 w-4" />
            </Button>
          }
        >
          <CreateDashboardForm onSuccess={() => setIsCreateOpen(false)} />
        </DialogFormWrapper>
      </div>

      <ScrollArea className="flex-1">
        <div className="px-1 flex flex-col gap-1">
          {dashboards?.items?.map((dashboard) => (
            <DashboardItem key={dashboard.id} dashboard={dashboard} />
          ))}
          {dashboards?.items?.length === 0 && (
            <div className="text-xs text-muted-foreground px-3 py-4 text-center italic">
              No dashboards found.
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

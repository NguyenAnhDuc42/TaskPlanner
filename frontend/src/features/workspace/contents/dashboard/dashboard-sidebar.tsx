import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useDashboards } from "./dashboard-api";
import { 
  Loader2, 
  Plus 
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useState } from "react";
import { EntityLayerType } from "@/types/relationship-type";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { DashboardItem } from "./dashboard-components/dashboard-item";
import { CreateDashboardForm } from "./dashboard-components/create-dashboard-form";

export function DashboardSidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: dashboards, isLoading } = useDashboards(workspaceId || "", EntityLayerType.ProjectWorkspace);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

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
        <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
          Dashboards
        </span>
        <DialogFormWrapper
          title="Create Dashboard"
          open={isCreateOpen}
          onOpenChange={setIsCreateOpen}
          trigger={
            <Button variant="ghost" size="icon" className="h-5 w-5 hover:bg-muted">
              <Plus className="h-3.5 w-3.5 text-muted-foreground" />
            </Button>
          }
        >
          <CreateDashboardForm onSuccess={() => setIsCreateOpen(false)} />
        </DialogFormWrapper>
      </div>

      <ScrollArea className="flex-1">
        <div className="px-1 flex flex-col gap-1">
          {dashboards?.map((dashboard) => (
            <DashboardItem key={dashboard.id} dashboard={dashboard} />
          ))}
          {dashboards?.length === 0 && (
            <div className="text-xs text-muted-foreground px-3 py-4 text-center italic">
              No dashboards found.
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

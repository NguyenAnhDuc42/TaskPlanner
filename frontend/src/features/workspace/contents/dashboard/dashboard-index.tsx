// @ts-ignore - react-grid-layout has a non-standard export structure
import { Responsive, WidthProvider } from "react-grid-layout/legacy";
import "/node_modules/react-grid-layout/css/styles.css";
import "/node_modules/react-resizable/css/styles.css";
import { useWidgets, useSaveDashboardLayout } from "./dashboard-api";
import { Route } from "@/routes/workspaces/$workspaceId";
import { Loader2, Plus, Settings, LayoutIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useMemo, useState } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateWidgetForm } from "./dashboard-components/create-widget-form";

const ResponsiveGridLayout = WidthProvider(Responsive);

export function DashboardIndex() {
  const { dashboardId } = Route.useSearch();
  const { data: widgets, isLoading } = useWidgets(dashboardId || "");
  const saveLayout = useSaveDashboardLayout();
  const [isAddWidgetOpen, setIsAddWidgetOpen] = useState(false);

  const layout = useMemo(() => {
    return (widgets || []).map(w => ({
      i: w.id,
      x: w.layout.col,
      y: w.layout.row,
      w: w.layout.width,
      h: w.layout.height,
    }));
  }, [widgets]);

  const onLayoutChange = (currentLayout: any) => {
    if (!dashboardId) return;
    
    const updates = currentLayout.map((l: any) => ({
      widgetId: l.i,
      col: l.x,
      row: l.y,
      width: l.w,
      height: l.h,
    }));

    saveLayout.mutate({ dashboardId, layouts: updates });
  };

  if (!dashboardId) {
    return (
      <div className="flex flex-col items-center justify-center h-[50vh] text-center gap-4">
        <div className="p-4 rounded-full bg-muted">
          <LayoutIcon className="h-8 w-8 text-muted-foreground" />
        </div>
        <div>
          <h3 className="text-lg font-semibold">Select a Dashboard</h3>
          <p className="text-sm text-muted-foreground">Select a dashboard from the sidebar to start.</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">Manage your workspace widgets.</p>
        </div>
        <div className="flex items-center gap-2">
          <DialogFormWrapper
            title="Add Widget"
            open={isAddWidgetOpen}
            onOpenChange={setIsAddWidgetOpen}
            trigger={
              <Button variant="outline" size="sm">
                <Plus className="h-4 w-4 mr-2" />
                Add Widget
              </Button>
            }
          >
            <CreateWidgetForm 
              dashboardId={dashboardId} 
              onSuccess={() => setIsAddWidgetOpen(false)} 
            />
          </DialogFormWrapper>
          <Button variant="ghost" size="icon">
            <Settings className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <ResponsiveGridLayout
        className="layout"
        layouts={{ lg: layout }}
        breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
        cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
        rowHeight={100}
        onLayoutChange={onLayoutChange}
        draggableHandle=".widget-header"
      >
        {widgets?.map((widget) => (
          <div key={widget.id} className="bg-card border rounded-xl shadow-sm overflow-hidden flex flex-col group">
            <div className="widget-header p-3 border-b bg-muted/20 flex items-center justify-between cursor-move opacity-0 group-hover:opacity-100 transition-opacity">
              <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                {widget.widgetType}
              </span>
              <Button variant="ghost" size="icon" className="h-5 w-5">
                <Settings className="h-3 w-3" />
              </Button>
            </div>
            <div className="flex-1 p-4 flex items-center justify-center italic text-muted-foreground text-sm">
              {widget.widgetType} Content
            </div>
          </div>
        ))}
      </ResponsiveGridLayout>
    </div>
  );
}


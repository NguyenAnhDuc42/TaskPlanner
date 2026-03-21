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
      WidgetId: l.i,
      Col: l.x,
      Row: l.y,
      Width: l.w,
      Height: l.h,
    }));

    saveLayout.mutate({ dashboardId, layouts: updates });
  };

  if (!dashboardId) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[500px] text-center gap-4">
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
      <div className="flex items-center justify-center min-h-[500px]">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6 h-full flex flex-col">
      <div className="flex items-center justify-between px-1">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-xs text-muted-foreground">Manage your workspace widgets.</p>
        </div>
        <div className="flex items-center gap-2">
          <DialogFormWrapper
            title="Add Widget"
            open={isAddWidgetOpen}
            onOpenChange={setIsAddWidgetOpen}
            trigger={
              <Button size="sm" className="h-8 gap-2 shadow-sm border-primary/20 hover:border-primary/40">
                <Plus className="h-4 w-4" />
                Add Widget
              </Button>
            }
          >
            <CreateWidgetForm 
              dashboardId={dashboardId} 
              onSuccess={() => setIsAddWidgetOpen(false)} 
            />
          </DialogFormWrapper>
          <Button variant="ghost" size="icon" className="h-8 w-8 group">
             <Settings className="h-4 w-4 transition-transform group-hover:rotate-90" />
          </Button>
        </div>
      </div>

      <div className="flex-1 relative min-h-[1000px] border rounded-xl overflow-hidden bg-muted/5">
        {/* Visual Grid Blocks Background */}
        <div 
          className="absolute inset-0 pointer-events-none opacity-[0.03]" 
          style={{ 
            backgroundImage: `
              linear-gradient(to right, currentColor 2px, transparent 2px),
              linear-gradient(to bottom, currentColor 2px, transparent 2px)
            `,
            backgroundSize: `${100 / 12}% 100px`,
            backgroundPosition: '-1px -1px'
          }} 
        />
        <div 
          className="absolute inset-0 pointer-events-none opacity-[0.02]" 
          style={{ 
            backgroundImage: `
              linear-gradient(to right, transparent 1px, rgba(0,0,0,0.1) 1px, rgba(0,0,0,0.1) calc(100% - 1px), transparent calc(100% - 1px)),
              linear-gradient(to bottom, transparent 1px, rgba(0,0,0,0.1) 1px, rgba(0,0,0,0.1) calc(100% - 1px), transparent calc(100% - 1px))
            `,
            backgroundSize: `${100 / 12}% 100px`,
            backgroundPosition: '-1px -1px'
          }} 
        />

        <ResponsiveGridLayout
          className="layout p-4"
          layouts={{ lg: layout }}
          breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
          cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
          rowHeight={100}
          onLayoutChange={onLayoutChange}
          draggableHandle=".widget-header"
        >
          {widgets?.map((widget) => (
            <div key={widget.id} className="bg-card border rounded-xl shadow-sm overflow-hidden flex flex-col group hover:ring-1 hover:ring-primary/40 transition-all">
              <div className="widget-header p-2.5 border-b bg-muted/20 flex items-center justify-between cursor-move">
                <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground truncate mr-2">
                  {widget.widgetType}
                </span>
                <Button variant="ghost" size="icon" className="h-5 w-5 flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
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
    </div>
  );
}


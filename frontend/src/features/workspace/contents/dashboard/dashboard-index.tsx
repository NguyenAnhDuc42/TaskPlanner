import { ReactGridLayout, WidthProvider } from "react-grid-layout/legacy";
import "/node_modules/react-grid-layout/css/styles.css";
import "/node_modules/react-resizable/css/styles.css";
import { useWidgets, useSaveDashboardLayout } from "./dashboard-api";
import { Route } from "@/routes/workspaces/$workspaceId";
import { Loader2, Plus, Settings, LayoutIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useMemo, useState, useEffect } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateWidgetForm } from "./dashboard-components/create-widget-form";
import { WidgetDataRenderer } from "./dashboard-components/widgets/widget-data-renderer";
import { signalRService } from "@/lib/signalr-service";
import { useAuth } from "@/features/auth/auth-context";
import type { WidgetData } from "./dashboard-type";

const GridLayout = WidthProvider(ReactGridLayout);

export function DashboardIndex() {
  const { dashboardId } = Route.useSearch();
  const { data: widgets, isLoading } = useWidgets(dashboardId || "");
  const saveLayout = useSaveDashboardLayout();
  const { user } = useAuth();
  const [isAddWidgetOpen, setIsAddWidgetOpen] = useState(false);
  
  // Reactive state for dynamic widget data (populated via SignalR)
  const [widgetDataMap, setWidgetDataMap] = useState<Record<string, WidgetData>>({});

  useEffect(() => {
    if (!dashboardId || !user?.id) return;

    const setupHub = async () => {
      try {
        await signalRService.startConnection();
        await signalRService.invoke("JoinDashboard", dashboardId);
        console.log("[DashboardHub] Joined dashboard group:", dashboardId);
      } catch (err) {
        console.error("[DashboardHub] Hub Setup Error:", err);
      }
    };

    setupHub();
    
    const onWidgetData = (data: WidgetData) => {
      console.log("[DashboardHub] Received Data:", data.widgetId);
      setWidgetDataMap(prev => ({ ...prev, [data.widgetId]: data }));
    };

    signalRService.on("WidgetDataLoaded", onWidgetData);
    
    return () => {
      signalRService.invoke("LeaveDashboard", dashboardId).catch(() => {});
      signalRService.off("WidgetDataLoaded", onWidgetData);
    };
  }, [dashboardId]);

  const layout = useMemo(() => {
    return (widgets?.items || []).map(w => ({
      i: w.id,
      x: w.layout.col,
      y: w.layout.row,
      w: w.layout.width,
      h: w.layout.height,
    }));
  }, [widgets?.items]);

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
    <div className="h-full flex flex-col overflow-hidden">
      {/* Sticky Top-Bar */}
      <div className="sticky top-0 z-20 flex items-center justify-between h-14 px-6 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 shrink-0">
        <div className="flex flex-col">
          <h1 className="text-sm font-semibold tracking-tight uppercase text-muted-foreground/70">Dashboard</h1>
          <div className="text-xs font-medium text-foreground/80 flex items-center gap-2">
            Workspace Hub
          </div>
        </div>
        <div className="flex items-center gap-2">
          <DialogFormWrapper
            title="Add Widget"
            open={isAddWidgetOpen}
            onOpenChange={setIsAddWidgetOpen}
            trigger={
              <Button size="sm" variant="outline" className="h-8 gap-2 rounded-md shadow-sm bg-primary hover:bg-primary/80">
                <Plus className="h-3.5 w-3.5 " />
                <span className="hidden sm:inline">Add Widget</span>
              </Button>
            }
          >
            <CreateWidgetForm 
              dashboardId={dashboardId} 
              onSuccess={() => setIsAddWidgetOpen(false)} 
            />
          </DialogFormWrapper>
          <Button variant="ghost" size="icon" className="h-8 w-8 group hover:bg-muted/50">
             <Settings className="h-3.5 w-3.5 transition-transform group-hover:rotate-90" />
          </Button>
        </div>
      </div>

      {/* Scrollable Layout Area */}
      <div className="flex-1 relative overflow-y-auto overflow-x-hidden min-h-0 bg-muted/[0.02]">
        {/* Visual Grid Blocks Background */}
        <div 
          className="absolute inset-0 pointer-events-none opacity-[0.04]" 
          style={{ 
            backgroundSize: `${100 / 12}% 110px`,
            padding: '16px',
            backgroundOrigin: 'content-box',
            backgroundRepeat: 'repeat',
            minHeight: '100%'
          }} 
        />

        <GridLayout
          className="layout p-4"
          layout={layout}
          cols={12}
          rowHeight={100}
          onLayoutChange={onLayoutChange}
          draggableHandle=".widget-header"
          compactType="vertical"
          preventCollision={false}
        >
          {widgets?.items?.map((widget) => {
            const dynamicData = widgetDataMap[widget.id];
            
            return (
              <div key={widget.id} className="bg-card border rounded-xl shadow-sm overflow-hidden flex flex-col group hover:ring-1 hover:ring-primary/40 transition-all">
                <div className="widget-header p-2.5 border-b bg-muted/20 flex items-center justify-between cursor-move">
                  <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground truncate mr-2">
                    {widget.widgetType}
                  </span>
                  <Button variant="ghost" size="icon" className="h-5 w-5 flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Settings className="h-3 w-3" />
                  </Button>
                </div>
                
                <div className="flex-1 overflow-hidden">
                   <WidgetDataRenderer widget={widget} data={dynamicData} />
                </div>
              </div>
            );
          })}
        </GridLayout>
      </div>
    </div>
  );
}


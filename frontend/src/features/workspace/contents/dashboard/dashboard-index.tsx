import { ReactGridLayout, WidthProvider } from "react-grid-layout/legacy";
import "/node_modules/react-grid-layout/css/styles.css";
import "/node_modules/react-resizable/css/styles.css";
import { useWidgets, useSaveDashboardLayout, useUpdateDashboard, useDeleteDashboard, useDeleteWidget, useDashboards } from "./dashboard-api";
import { Route } from "@/routes/workspaces/$workspaceId";
import { Loader2, Plus, Settings, LayoutIcon, MoreVertical, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useMemo, useState, useEffect } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateWidgetForm } from "./dashboard-components/create-widget-form";
import { WidgetDataRenderer } from "./dashboard-components/widgets/widget-data-renderer";
import { signalRService } from "@/lib/signalr-service";
import { useAuth } from "@/features/auth/auth-context";
import type { WidgetData } from "./dashboard-type";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { toast } from "sonner";
import { EntityLayerType } from "@/types/relationship-type";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const GridLayout = WidthProvider(ReactGridLayout);

export function DashboardIndex() {
  const { workspaceId } = Route.useParams();
  const { dashboardId } = Route.useSearch();
  const layerType = EntityLayerType.ProjectWorkspace;
  const { data: dashboards } = useDashboards(workspaceId, layerType);
  const { data: widgets, isLoading } = useWidgets(dashboardId || "");
  const saveLayout = useSaveDashboardLayout();
  const updateDashboard = useUpdateDashboard();
  const deleteDashboard = useDeleteDashboard();
  const deleteWidget = useDeleteWidget();
  
  const { user } = useAuth();
  const [isAddWidgetOpen, setIsAddWidgetOpen] = useState(false);
  
  // Custom Shadcn Dialog States
  const [isRenameOpen, setIsRenameOpen] = useState(false);
  const [renameInput, setRenameInput] = useState("");
  
  const [isDeleteDashboardOpen, setIsDeleteDashboardOpen] = useState(false);
  const [widgetToDelete, setWidgetToDelete] = useState<string | null>(null);

  const currentDashboard = useMemo(() => {
    return dashboards?.items.find(d => d.id === dashboardId);
  }, [dashboards?.items, dashboardId]);
  
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
      {/* SHADCN DIALOGS */}
      <Dialog open={isRenameOpen} onOpenChange={setIsRenameOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Rename Dashboard</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-4 items-center gap-4">
              <Label htmlFor="name" className="text-right">Name</Label>
              <Input
                id="name"
                value={renameInput}
                onChange={(e) => setRenameInput(e.target.value)}
                className="col-span-3"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsRenameOpen(false)}>Cancel</Button>
            <Button onClick={() => {
              if (renameInput.trim()) {
                updateDashboard.mutate({ dashboardId: dashboardId!, name: renameInput.trim() }, {
                  onSuccess: () => {
                    toast.success("Dashboard renamed");
                    setIsRenameOpen(false);
                  }
                });
              }
            }}>Save changes</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog open={isDeleteDashboardOpen} onOpenChange={setIsDeleteDashboardOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete your dashboard
              and remove all its widgets.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                deleteDashboard.mutate({ id: dashboardId!, layerId: workspaceId, layerType }, {
                  onSuccess: () => {
                    toast.success("Dashboard deleted");
                    setIsDeleteDashboardOpen(false);
                  }
                });
              }}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={!!widgetToDelete} onOpenChange={(open) => !open && setWidgetToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Widget?</AlertDialogTitle>
            <AlertDialogDescription>
              This will remove the widget from the dashboard. You can always add it back later.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                if (widgetToDelete) {
                  deleteWidget.mutate({ dashboardId: dashboardId!, widgetId: widgetToDelete }, {
                    onSuccess: () => {
                      toast.success("Widget removed");
                      setWidgetToDelete(null);
                    }
                  });
                }
              }}
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Sticky Top-Bar */}
      <div className="sticky top-0 z-20 flex items-center justify-between h-14 px-6 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 shrink-0">
        <div className="flex flex-col">
          <h1 className="text-sm font-semibold tracking-tight uppercase text-muted-foreground/70">Dashboard</h1>
          <div className="text-xs font-medium text-foreground/80 flex items-center gap-2">
            {currentDashboard?.name || "Workspace Hub"}
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
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8 group hover:bg-muted/50 data-[state=open]:bg-muted">
                <MoreVertical className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
              <DropdownMenuItem 
                onClick={() => {
                  setRenameInput(currentDashboard?.name || "");
                  setIsRenameOpen(true);
                }}
                className="cursor-pointer gap-2"
              >
                <Pencil className="h-3.5 w-3.5" />
                <span>Rename Dashboard</span>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem 
                className="cursor-pointer gap-2 text-destructive focus:bg-destructive/10 focus:text-destructive"
                onClick={() => setIsDeleteDashboardOpen(true)}
              >
                <Trash2 className="h-3.5 w-3.5" />
                <span>Delete Dashboard</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
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
          compactType="horizontal"
          preventCollision={false}
          resizeHandles={['s', 'w', 'e', 'n', 'sw', 'nw', 'se', 'ne']}
          resizeHandle={(axis, ref) => (
            <span
              ref={ref}
              className={`react-resizable-handle react-resizable-handle-${axis} opacity-0`}/>
          )}
        >
          {widgets?.items?.map((widget) => {
            const dynamicData = widgetDataMap[widget.id];
            
            return (
              <div key={widget.id} className="bg-card border rounded-xl shadow-sm overflow-hidden flex flex-col group hover:ring-1 hover:ring-primary/40 transition-all">
                <div className="widget-header p-2.5 border-b bg-muted/20 flex items-center justify-between cursor-move">
                  <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground truncate mr-2">
                    {widget.widgetType}
                  </span>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon" className="h-5 w-5 flex-shrink-0 opacity-0 group-hover:opacity-100 transition-opacity data-[state=open]:opacity-100">
                        <MoreVertical className="h-3 w-3" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem className="cursor-pointer gap-2" onClick={() => toast.info("Widget config edit not yet implemented")}>
                        <Settings className="h-3.5 w-3.5" />
                        <span>Edit Config</span>
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem 
                        className="cursor-pointer gap-2 text-destructive focus:bg-destructive/10 focus:text-destructive"
                        onClick={() => setWidgetToDelete(widget.id)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                        <span>Remove Widget</span>
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
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


import { ReactGridLayout, WidthProvider } from "react-grid-layout/legacy";
import "/node_modules/react-grid-layout/css/styles.css";
import "/node_modules/react-resizable/css/styles.css";
import { useWidgets, useSaveDashboardLayout, useUpdateDashboard, useDeleteDashboard, useDeleteWidget, useDashboards } from "./dashboard-api";
import { Route } from "@/routes/workspaces/$workspaceId";
import { Loader2, Plus, Settings, LayoutIcon, MoreVertical, Pencil, Trash2, Maximize2, GripVertical } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useMemo, useState, useEffect } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateWidgetForm } from "./dashboard-components/create-widget-form";
import { WidgetDataRenderer } from "./dashboard-components/widgets/widget-data-renderer";
import { signalRService } from "@/lib/signalr-service";
import { useAuth } from "@/features/auth/auth-context";
import type { WidgetData, WidgetDto } from "./dashboard-type";
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
import { WidgetDetailDialog } from "./dashboard-components/widgets/widget-detail-dialog";

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
  const [expandedWidget, setExpandedWidget] = useState<{ widget: WidgetDto; data?: WidgetData } | null>(null);

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
      <div className="flex flex-col items-center justify-center min-h-[500px] text-center gap-6 animate-in fade-in slide-in-from-bottom-4 duration-700">
        <div className="p-6 rounded-full bg-[var(--theme-item-normal)] border border-border/10">
          <LayoutIcon className="h-10 w-10 text-[var(--theme-text-hover)] opacity-60" />
        </div>
        <div className="space-y-1.5">
          <h3 className="text-lg font-bold uppercase tracking-[0.3em] text-[var(--theme-text-hover)]">Select a Dashboard</h3>
          <p className="text-[10px] font-bold uppercase tracking-[0.1em] text-[var(--theme-text-normal)] opacity-40 italic">Initiate a workspace segment to begin.</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[500px]">
        <Loader2 className="h-8 w-8 animate-spin text-[var(--theme-text-normal)]" />
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

      <WidgetDetailDialog
        open={!!expandedWidget}
        onOpenChange={(open) => !open && setExpandedWidget(null)}
        widget={expandedWidget?.widget ?? null}
        data={expandedWidget?.data}
      />

      {/* Sticky Top-Bar */}
      <div className="sticky top-0 z-20 flex items-center justify-between h-14 px-6 border-b border-border/10 bg-[var(--glass-bg)] backdrop-blur-xl shrink-0">
        <div className="flex flex-col">
          <h1 className="text-[10px] font-bold uppercase tracking-[0.3em] text-[var(--theme-text-normal)] opacity-60">Dashboard</h1>
          <div className="text-xs font-bold uppercase tracking-[0.1em] text-[var(--theme-text-hover)] flex items-center gap-2">
            {currentDashboard?.name || "Workspace Hub"}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <DialogFormWrapper
            title="Add Widget"
            open={isAddWidgetOpen}
            onOpenChange={setIsAddWidgetOpen}
            trigger={
              <Button size="sm" variant="ghost" className="h-8 gap-2 rounded-md theme-selected transition-all hover:scale-[1.02] border-0">
                <Plus className="h-3.5 w-3.5 " />
                <span className="hidden sm:inline">Add Widget</span>
              </Button>
            }
          >
            <CreateWidgetForm 
              dashboardId={dashboardId || ""} 
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
      <div className="flex-1 relative overflow-y-auto overflow-x-hidden min-h-0 bg-transparent">
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
          draggableHandle=".widget-grip-handle"
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
              <div key={widget.id} className="glass-panel rounded-md shadow-sm overflow-hidden flex flex-col group hover:ring-[var(--theme-border-crisp)] transition-all hover:scale-[1.01] hover:shadow-xl">
                <div className="widget-header p-2.5 border-b border-border/10 bg-transparent flex items-center justify-between cursor-move">
                  <div className="flex items-center gap-1.5 truncate">
                    <div className="widget-grip-handle">
                      <GripVertical className="h-3.5 w-3.5 text-[var(--theme-text-normal)] opacity-30 group-hover:opacity-100 flex-shrink-0" />
                    </div>
                    <span className="text-[10px] font-bold uppercase tracking-[0.2em] text-[var(--theme-text-normal)] truncate">
                      {widget.widgetType}
                    </span>
                  </div>
                  <div className="flex items-end gap-2">
                  <Button 
                    variant="ghost" 
                    size="icon" 
                    className="h-5 w-5 flex-shrink-0 text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] opacity-0 group-hover:opacity-100 transition-opacity"
                    onClick={() => setExpandedWidget({ widget, data: dynamicData })}
                  >
                    <Maximize2 className="h-3 w-3" />
                  </Button>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon" className="h-5 w-5 flex-shrink-0 text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] opacity-0 group-hover:opacity-100 transition-opacity data-[state=open]:opacity-100">
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


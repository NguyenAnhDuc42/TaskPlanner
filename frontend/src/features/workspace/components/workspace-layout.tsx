import { Suspense } from "react";
import { useLocation, useNavigate, Outlet } from "@tanstack/react-router";
import { useWorkspaceSession } from "../context/workspace-session";
import { SidebarRegistry } from "./sidebar-registry";
import { IconRail } from "./icon-rail";
import { ContextPanelRenderer } from "./context-panel-renderer";
import { useResize } from "@/hooks/use-resize";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { ChevronLeft, X } from "lucide-react";
import type { ContentPage } from "../type";

export function WorkspaceLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { state, actions } = useWorkspaceSession();

  // Context panel data from router search state
  const contextData = (location.search as any)?.contextPanel;
  const isContextOpen = !!contextData;

  const handleSelectIcon = (icon: ContentPage) => {
    actions.selectIcon(icon);
    (navigate as any)({
      to: `/workspaces/$workspaceId/${icon}`,
      params: { workspaceId: (location as any).params?.workspaceId },
      search: {},
    });
  };

  const handleCommandCenter = () => {
    // Clear active icon, close sidebar, navigate to workspace home
    actions.selectIcon(null);
    (navigate as any)({
      to: "/workspaces/$workspaceId",
      params: { workspaceId: (location as any).params?.workspaceId },
      search: {},
    });
  };

  const handleCloseContextPanel = () => {
    const newSearch = { ...(location.search as Record<string, unknown>) };
    delete newSearch.contextPanel;
    (navigate as any)({ search: newSearch });
  };

  const {
    width: sidebarWidth,
    isResizing: isResizingSidebar,
    startResizing: startResizingSidebar,
  } = useResize({
    initialWidth: state.sidebarWidth,
    minWidth: 200,
    maxWidth: 500,
    direction: "left",
    offset: 60,
    onResizeEnd: (newWidth) => actions.updateSidebarWidth(newWidth),
  });

  const {
    width: contextWidth,
    isResizing: isResizingContext,
    startResizing: startResizingContext,
  } = useResize({
    initialWidth: state.contextWidth,
    minWidth: 250,
    maxWidth: 800,
    direction: "right",
    offset: 16,
    onResizeEnd: (newWidth) => actions.updateContextWidth(newWidth),
  });

  return (
    <div className="relative flex h-screen w-full overflow-hidden p-2 gap-2 bg-background">
      {/* ═══════════════════════════════════════════════════
          COLUMN 1: Icon Rail
      ═══════════════════════════════════════════════════ */}
      <IconRail
        onSelectIcon={handleSelectIcon}
        onCommandCenter={handleCommandCenter}
      />

      {/* Hover Peek Frame */}
      {state.hoveredIcon && !state.isInnerSidebarOpen && (
        <div
          className="absolute top-2 left-[60px] h-[calc(100%-16px)] w-64 z-50 animate-in fade-in slide-in-from-left-2 duration-200"
          onMouseEnter={() => actions.setHoveredIcon(state.hoveredIcon)}
          onMouseLeave={() => actions.setHoveredIcon(null)}
        >
          <div className="h-full w-full bg-background border border-border rounded-md shadow-xl flex flex-col overflow-hidden">
            <div className="flex items-center justify-between px-2 py-2 flex-shrink-0 border-b border-border">
              <h2 className="font-black text-sm uppercase tracking-widest text-foreground">
                {state.hoveredIcon}
              </h2>
            </div>
            <ScrollArea className="flex-1 min-h-0">
              <div className="flex-1 p-1 min-h-0 flex flex-col overflow-hidden">
                <SidebarRegistry page={state.hoveredIcon} />
              </div>
            </ScrollArea>
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════
          COLUMN 2: Inner Sidebar
      ═══════════════════════════════════════════════════ */}
      {state.activeIcon && (
        <div
          style={{ width: state.isInnerSidebarOpen ? sidebarWidth : 0 }}
          className={cn(
            "transition-all duration-300 flex flex-col h-full flex-shrink-0 relative overflow-hidden",
            "bg-background border border-border rounded-md shadow-sm",
            state.isInnerSidebarOpen
              ? "opacity-100"
              : "opacity-0 pointer-events-none",
          )}
        >
          <div className="h-12 flex items-center justify-between px-4 flex-shrink-0 border-b border-border">
            <h2 className="font-black text-[11px] uppercase tracking-widest text-foreground">
              {state.activeIcon}
            </h2>
            <Button
              size="icon"
              variant="ghost"
              onClick={actions.toggleInnerSidebar}
              className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
            </Button>
          </div>

          <div className="flex-1 p-1 min-h-0 overflow-hidden">
            <SidebarRegistry page={state.activeIcon} />
          </div>

          {/* Resize Handle */}
          <div
            onMouseDown={startResizingSidebar}
            className="absolute top-0 right-0 w-3 h-full cursor-col-resize z-50 group flex justify-end"
          >
            <div
              className={cn(
                "h-full w-0.5 transition-colors",
                isResizingSidebar
                  ? "bg-primary"
                  : "bg-transparent group-hover:bg-primary/50",
              )}
            />
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════
          COLUMN 3: Main Canvas
      ═══════════════════════════════════════════════════ */}
      <div className="flex-1 min-w-0 h-full flex flex-col relative">
        <Suspense
          fallback={
            <div className="flex m-6 p-8 items-center justify-center text-sm font-mono tracking-widest uppercase text-muted-foreground/60 w-full animate-pulse rounded-md">
              Synchronizing Nodes...
            </div>
          }
        >
          <Outlet />
        </Suspense>
      </div>

      {/* ═══════════════════════════════════════════════════
          COLUMN 4: Context Panel (from router search state)
      ═══════════════════════════════════════════════════ */}
      {isContextOpen && (
        <div
          style={{ width: contextWidth }}
          className="transition-all duration-300 flex flex-col h-full flex-shrink-0 relative overflow-hidden bg-background border border-border rounded-md shadow-sm"
        >
          <div className="h-12 flex items-center justify-between px-4 flex-shrink-0 border-b border-border">
            <h2 className="font-black text-[11px] uppercase tracking-widest text-foreground">
              Details
            </h2>
            <Button
              size="icon"
              variant="ghost"
              onClick={handleCloseContextPanel}
              className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
            >
              <X className="h-3.5 w-3.5" />
            </Button>
          </div>

          <div className="flex-1 p-2 min-h-0 overflow-auto">
            <ContextPanelRenderer data={contextData} />
          </div>

          {/* Resize Handle */}
          <div
            onMouseDown={startResizingContext}
            className="absolute top-0 left-0 w-3 h-full cursor-col-resize z-50 group flex justify-start"
          >
            <div
              className={cn(
                "h-full w-0.5 transition-colors",
                isResizingContext
                  ? "bg-primary"
                  : "bg-transparent group-hover:bg-primary/50",
              )}
            />
          </div>
        </div>
      )}
    </div>
  );
}

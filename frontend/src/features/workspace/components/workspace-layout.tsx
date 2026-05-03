import { Suspense } from "react";
import { useLocation, useNavigate, Outlet } from "@tanstack/react-router";
import { useWorkspace } from "../context/workspace-provider";
import { SidebarRegistry } from "./sidebar-registry";
import { IconRail } from "./icon-rail";
import { ContextPanelRenderer } from "./context-panel-renderer";
import { useResize } from "@/hooks/use-resize";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ChevronLeft, X } from "lucide-react";
import type { ContentPage } from "../type";

export function WorkspaceLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { workspaceId, ui, actions } = useWorkspace();

  // ─── Context Panel (from router search) ──────────────
  const contextData = (location.search as any)?.contextPanel;
  const isContextOpen = !!contextData;

  // ─── Navigation Handlers ────────────────────────────
  const handleSelectIcon = (icon: ContentPage) => {
    const targetRoute = icon === "projects" ? "" : icon;
    (navigate as any)({
      to: `/workspaces/$workspaceId/${targetRoute}`,
      params: { workspaceId },
      search: {}, // Close context panel when switching icons
    });
  };

  const handleCloseContextPanel = () => {
    navigate({
      to: location.pathname,
      search: (prev: any) => {
        const { contextPanel, ...rest } = prev;
        return rest;
      },
    });
  };

  // ─── Resize Handlers ────────────────────────────────
  const {
    width: sidebarWidth,
    isResizing: isResizingSidebar,
    startResizing: startResizingSidebar,
  } = useResize({
    initialWidth: ui.sidebarWidth,
    minWidth: 50,
    maxWidth: 500,
    direction: "left",
    onResize: (newWidth) => {
      if (newWidth === 0 && ui.isInnerSidebarOpen) {
        actions.setSidebarOpenLocal(false);
      } else if (newWidth > 0 && !ui.isInnerSidebarOpen) {
        actions.setSidebarOpenLocal(true);
      }
      actions.setSidebarWidthLocal(newWidth);
    },
    onResizeEnd: (newWidth) => {
      actions.updateSidebarWidth(newWidth);
    },
  });

  const {
    width: contextWidth,
    isResizing: isResizingContext,
    startResizing: startResizingContext,
  } = useResize({
    initialWidth: ui.contextWidth,
    minWidth: 50,
    maxWidth: 800,
    direction: "right",
    onResize: (newWidth) => {
      actions.setContextWidthLocal(newWidth);
    },
    onResizeEnd: (newWidth) => {
      actions.updateContextWidth(newWidth);
    },
  });

  const handleCommandCenter = () => {
    navigate({
      to: "/workspaces/$workspaceId/command-center",
      params: { workspaceId },
    });
  };

  return (
    <div className="relative flex h-screen w-full overflow-hidden p-2 gap-2 bg-background font-sans">
      {/* ═══════════════════════════════════════════════════
          COLUMN 1: Icon Rail
      ═══════════════════════════════════════════════════ */}
      <IconRail onSelectIcon={handleSelectIcon} onCommandCenter={handleCommandCenter} />

      {/* ─── Hover Peek Frame ───────────────────────────── */}
      {ui.hoveredIcon && !ui.isInnerSidebarOpen && (
        <div
          className="absolute top-2 left-[60px] h-[calc(100%-16px)] w-64 z-50 animate-in fade-in slide-in-from-left-2 duration-200"
          onMouseEnter={() => actions.setHoveredIcon(ui.hoveredIcon)}
          onMouseLeave={() => actions.setHoveredIcon(null)}
        >
          <div className="h-full w-full bg-background border border-border rounded-md shadow-xl flex flex-col overflow-hidden">
            <div className="flex items-center justify-between px-3 py-2 flex-shrink-0 border-b border-border bg-muted/30">
              <h2 className="font-black text-[10px] uppercase tracking-widest text-foreground/70">
                Quick Look: {ui.hoveredIcon}
              </h2>
            </div>
            <div className="flex-1 min-h-0 overflow-hidden p-1">
              <SidebarRegistry page={ui.hoveredIcon} />
            </div>
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════
          COLUMN 2: Inner Sidebar (resizable)
      ═══════════════════════════════════════════════════ */}
      {ui.isInnerSidebarOpen && (
        <div
          style={{ width: isResizingSidebar ? sidebarWidth : ui.sidebarWidth }}
          className={cn(
            "flex flex-col h-full flex-shrink-0 relative overflow-hidden",
            "bg-background border border-border rounded-md shadow-sm",
            !isResizingSidebar && "transition-all duration-300",
          )}
        >
          <div className="h-12 flex items-center justify-between px-4 flex-shrink-0 border-b border-border bg-muted/10">
            <h2 className="font-black text-[11px] uppercase tracking-widest text-foreground">
              {ui.activeIcon}
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
            <SidebarRegistry page={ui.activeIcon} />
          </div>

          {/* Resize Handle */}
          <div
            onMouseDown={startResizingSidebar}
            className={cn(
              "absolute top-0 right-0 w-1.5 h-full cursor-col-resize z-50 group touch-none",
              isResizingSidebar && "z-[100]",
            )}
          >
            <div
              className={cn(
                "h-full w-[2px] mx-auto transition-colors duration-200",
                isResizingSidebar
                  ? "bg-primary"
                  : "group-hover:bg-primary/40 bg-transparent",
              )}
            />
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════
          COLUMN 3: Main Canvas
      ═══════════════════════════════════════════════════ */}
      <div className="flex-1 min-w-0 h-full flex flex-col relative bg-background border border-border rounded-md shadow-sm">
        <Suspense
          fallback={
            <div className="flex h-full w-full items-center justify-center text-sm font-mono tracking-widest uppercase text-muted-foreground/60 animate-pulse">
              Synchronizing Nodes...
            </div>
          }
        >
          <Outlet />
        </Suspense>
      </div>

      {/* ═══════════════════════════════════════════════════
          COLUMN 4: Context Panel (resizable)
      ═══════════════════════════════════════════════════ */}
      {isContextOpen && (
        <div
          style={{ width: isResizingContext ? contextWidth : ui.contextWidth }}
          className={cn(
            "flex flex-col h-full flex-shrink-0 relative overflow-hidden",
            "bg-background border border-border rounded-md shadow-sm",
            !isResizingContext && "transition-all duration-300",
          )}
        >
          <div className="h-12 flex items-center justify-between px-4 flex-shrink-0 border-b border-border bg-muted/10">
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

import { lazy, Suspense, useEffect, useState } from "react";
import { useLocation, useNavigate, Outlet } from "@tanstack/react-router";
import { useWorkspace } from "../context/workspace-context";
import { AppSidebar } from "./app-sidebar";
import { ContextPanelRenderer } from "./context-panel-renderer";
import { ResizablePanel } from "./resizable-panel";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { useResize } from "@/hooks/use-resize";
import { useIsMobile } from "@/hooks/use-mobile";
import { Button } from "@/components/ui/button";
import { LoadingScreen } from "@/components/loading-screen";
import { ChevronLeft, X, Maximize2 } from "lucide-react";
import { NotificationBell } from "@/features/notifications/notification-bell";
import type { ContentPage } from "../type";
import { ProfileModal } from "@/features/auth/profile/components/profile-modal";
import { MobileTabBar } from "./mobile/mobile-tab-bar";
import { MobileSidebarDrawer } from "./mobile/mobile-sidebar-drawer";
import { OfflineBanner } from "./offline-banner";
import { DocumentEditorProvider } from "../context/document-editor-context";
import { UserMenu } from "./user-menu";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { cn } from "@/lib/utils";
import { CommandPalette } from "./command-palette";

// Below this width the sidebar renders its collapsed icon-only variant instead of shrinking labels.
const SIDEBAR_COLLAPSE_BREAKPOINT = 160;
const SIDEBAR_MIN_WIDTH = 40;
const SIDEBAR_MAX_WIDTH = 360;
const SIDEBAR_DEFAULT_WIDTH = 260;

const DocumentEditorHost = lazy(() =>
  import("@/components/blockbase/document-editor-host").then((m) => ({ default: m.DocumentEditorHost })),
);

function useWindowWidth() {
  const [width, setWidth] = useState(() => globalThis.innerWidth ?? 1350);
  useEffect(() => {
    const onResize = () => setWidth(globalThis.innerWidth);
    globalThis.addEventListener("resize", onResize);
    return () => globalThis.removeEventListener("resize", onResize);
  }, []);
  return width;
}

function closeContextPanelSearch(prev: Record<string, unknown>) {
  const next = { ...prev };
  delete next.contextPanel;
  return next;
}

export function WorkspaceLayout() {
  return (
    <DocumentEditorProvider>
      <WorkspaceLayoutInner />
      <Suspense fallback={null}>
        <DocumentEditorHost />
      </Suspense>
      <CommandPalette />
    </DocumentEditorProvider>
  );
}

function WorkspaceLayoutInner() {
  const [profileOpen, setProfileOpen] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const isMobile = useIsMobile();

  const navigate = useNavigate({ from: "/workspaces/$workspaceId" });
  const location = useLocation();
  const { workspaceId, ui, actions } = useWorkspace();
  const rootStore = useWorkspaceRootStore();
  const windowWidth = useWindowWidth();

  const contextData = location.search?.contextPanel;
  const isContextOpen = !!contextData;

  // Mobile tab bar still switches between top-level "pages" (projects/my-tasks/inbox/...) —
  // the desktop sidebar replaced this concept with persistent nav, but mobile keeps the old paging.
  const handleSelectIcon = (icon: ContentPage) => {
    if (icon === "projects") {
      const spaces = rootStore.spaceStore.all;
      if (spaces.length > 0) {
        const lastSpaceId = localStorage.getItem(`lastSpaceId:${workspaceId}`);
        const targetSpaceId = lastSpaceId && spaces.some((s) => s.id === lastSpaceId)
          ? lastSpaceId
          : rootStore.spaceStore.allSorted[0].id;

        navigate({
          to: "/workspaces/$workspaceId/spaces/$spaceId",
          params: { workspaceId, spaceId: targetSpaceId },
          search: {},
        });
        return;
      }
    }

    const targetRoute = icon === "projects" ? "" : icon;
    navigate({
      to: `/workspaces/$workspaceId/${targetRoute}`,
      params: { workspaceId },
      search: {}, // Close context panel when switching icons
    });
  };

  const handleCloseContextPanel = () => {
    navigate({ to: location.pathname, search: closeContextPanelSearch });
  };

  const handleExpandContextPanel = () => {
    if (!contextData) return;
    if (contextData.type === "task" && contextData.id) {
      navigate({
        to: `/workspaces/$workspaceId/tasks/$taskId`,
        params: { workspaceId, taskId: contextData.id },
        search: closeContextPanelSearch,
      });
    }
  };

  const {
    width: sidebarWidth,
    isResizing: isResizingSidebar,
    startResizing: startResizingSidebar,
  } = useResize({
    initialWidth: ui.sidebarWidth,
    minWidth: SIDEBAR_MIN_WIDTH,
    maxWidth: SIDEBAR_MAX_WIDTH,
    direction: "left",
    onResizeEnd: (newWidth) => actions.updateSidebarWidth(newWidth),
  });

  const rawSidebarWidth = isResizingSidebar ? sidebarWidth : ui.sidebarWidth;
  const sidebarCollapsed = rawSidebarWidth < SIDEBAR_COLLAPSE_BREAKPOINT;
  const sidebarDisplayWidth = sidebarCollapsed ? SIDEBAR_MIN_WIDTH : rawSidebarWidth;
  const handleExpandSidebar = () => actions.updateSidebarWidth(SIDEBAR_DEFAULT_WIDTH);
  const handleCollapseSidebar = () => actions.updateSidebarWidth(SIDEBAR_MIN_WIDTH);

  const availableWidth = windowWidth - sidebarDisplayWidth;
  const maxContextWidth = availableWidth - 10; // Exactly 10px left for the main area
  const expandThreshold = maxContextWidth - 10;

  const {
    width: contextWidth,
    isResizing: isResizingContext,
    startResizing: startResizingContext,
  } = useResize({
    initialWidth: ui.contextWidth === 0 ? 300 : ui.contextWidth,
    currentWidth: ui.contextWidth === 0 ? 300 : ui.contextWidth,
    minWidth: 10,
    maxWidth: maxContextWidth,
    direction: "right",
    onResize: (newWidth) => {
      if (newWidth === 0) {
        if (contextData) handleCloseContextPanel();
      } else if (newWidth >= expandThreshold) {
        if (contextData) handleExpandContextPanel();
      }
    },
    onResizeEnd: (newWidth) => {
      if (newWidth >= expandThreshold) {
        actions.updateContextWidth(300);
      } else if (newWidth === 0) {
        actions.updateContextWidth(300);
      } else {
        actions.updateContextWidth(newWidth);
      }
    },
  });

  if (isMobile) {
    return (
      <div className="flex h-screen w-full flex-col p-1 gap-1 bg-background font-sans overflow-hidden">
        <OfflineBanner />
        <header className="h-9 w-full shrink-0 flex items-center justify-between px-2 bg-card border border-border rounded-md shadow-sm">
          <WorkspaceSwitcher />
          <div className="flex items-center gap-1.5">
            <NotificationBell />
            <div className="h-6 w-px bg-border/50 mx-1" />
            <UserMenu onOpenProfile={() => setProfileOpen(true)} />
          </div>
        </header>

        <div className="flex-1 min-h-0 flex flex-col relative bg-card border border-border rounded-md shadow-sm overflow-hidden">
          <Suspense fallback={<LoadingScreen label="Loading" />}>
            <Outlet />
          </Suspense>
        </div>

        <MobileTabBar onSelectIcon={handleSelectIcon} onOpenDrawer={() => setIsDrawerOpen(true)} />
        <MobileSidebarDrawer open={isDrawerOpen} onOpenChange={setIsDrawerOpen} />

        <ProfileModal open={profileOpen} onOpenChange={setProfileOpen} />
      </div>
    );
  }

  return (
    <div className="flex h-screen w-full flex-col pl-1 pr-2 py-2 gap-1 bg-background font-sans overflow-hidden">
      <OfflineBanner />

      <div className="flex-1 flex gap-1 min-h-0 relative">
        {/* ═══════════════════════════════════════════════════
            COLUMN 1: Sidebar — workspace switcher, search, inbox,
            my tasks, favorites, project list, user menu. Flush against
            the page background (no card chrome) so it reads as the
            frame the main canvas floats inside, not a sibling panel.
        ═══════════════════════════════════════════════════ */}
        <div
          className={cn("relative h-full shrink-0", !isResizingSidebar && "transition-all duration-200 ease-in-out")}
          style={{ width: sidebarDisplayWidth }}
        >
          <AppSidebar
            onOpenProfile={() => setProfileOpen(true)}
            collapsed={sidebarCollapsed}
            onExpand={handleExpandSidebar}
            onCollapse={handleCollapseSidebar}
          />
          <div
            onMouseDown={startResizingSidebar}
            className={cn(
              "absolute top-0 -right-[3px] w-[6px] h-full cursor-col-resize z-50 group touch-none",
              isResizingSidebar && "z-[100]",
            )}
          >
            <div
              className={cn(
                "h-full w-[1.5px] mx-auto transition-colors duration-200",
                isResizingSidebar ? "bg-primary" : "group-hover:bg-primary/50 bg-transparent",
              )}
            />
          </div>
        </div>

        {/* ═══════════════════════════════════════════════════
            COLUMN 2: Main Canvas
        ═══════════════════════════════════════════════════ */}
        <div className="flex-1 min-w-0 h-full flex flex-col relative bg-card border border-border rounded-md shadow-sm overflow-hidden">
          <Suspense fallback={<LoadingScreen label="Loading" />}>
            <Outlet />
          </Suspense>
        </div>

        {/* ═══════════════════════════════════════════════════
            COLUMN 3: Context Panel (resizable)
        ═══════════════════════════════════════════════════ */}
        {isContextOpen && (
          <ResizablePanel
            width={isResizingContext ? contextWidth : ui.contextWidth}
            isResizing={isResizingContext}
            onResizeStart={startResizingContext}
            handleSide="left"
          >
            <div className="h-8 flex items-center justify-between px-2 flex-shrink-0 border-b border-border bg-card/30 gap-1">
              <div className="flex items-center gap-1">
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={() => window.history.back()}
                  className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                  title="Go back"
                >
                  <ChevronLeft className="h-3.5 w-3.5" />
                </Button>
              </div>

              <div className="flex items-center gap-1 ml-auto">
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={handleExpandContextPanel}
                  className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                  title="Open in full view"
                >
                  <Maximize2 className="h-3.5 w-3.5" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={handleCloseContextPanel}
                  className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                  title="Close"
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>

            <div className="flex-1 p-1 min-h-0 overflow-auto bg-card/30">
              <ContextPanelRenderer data={contextData} />
            </div>
          </ResizablePanel>
        )}
      </div>

      <ProfileModal open={profileOpen} onOpenChange={setProfileOpen} />
    </div>
  );
}

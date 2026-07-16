import { lazy, Suspense, useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, Outlet } from "@tanstack/react-router";
import { useWorkspace } from "../context/workspace-context";
import { HierarchySidebar } from "../contents/hierarchy/hierarchy-sidebar";
import { IconRail } from "./icon-rail";
import { ContextPanelRenderer } from "./context-panel-renderer";
import { ResizablePanel } from "./resizable-panel";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { useResize } from "@/hooks/use-resize";
import { useIsMobile } from "@/hooks/use-mobile";
import { Button } from "@/components/ui/button";
import { LoadingScreen } from "@/components/loading-screen";
import { ChevronLeft, X, Maximize2, LogOut, User } from "lucide-react";
import { NotificationBell } from "@/features/notifications/notification-bell";
import { GlobalSearch } from "./global-search";
import { UserAvatar } from "@/components/user-avatar";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import type { ContentPage } from "../type";
import { useLogout, useUser } from "@/features/auth/auth-api";
import { ProfileModal } from "@/features/auth/profile/components/profile-modal";
import { MobileTabBar } from "./mobile/mobile-tab-bar";
import { MobileSidebarDrawer } from "./mobile/mobile-sidebar-drawer";
import { OfflineBanner } from "./offline-banner";
import { DocumentEditorProvider } from "../context/document-editor-context";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const HIERARCHY_PAGES = new Set<ContentPage>(["projects", "spaces", "folders", "tasks"]);
const ICON_RAIL_WIDTH = 55;

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

function UserMenu({ onOpenProfile }: { onOpenProfile: () => void }) {
  const { data: user } = useUser();
  const { mutate: logout } = useLogout()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className="outline-none cursor-pointer hover:opacity-80 transition-opacity">
          <UserAvatar
            name={user?.name || "User"}
            avatarUrl={null}
            className="h-7 w-7 rounded-md"
            fallbackClassName="text-[10px] rounded-md shadow-sm"
          />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-52 p-0 overflow-hidden">
        <DropdownMenuLabel className="px-2 py-1.5">
          <p className="text-xs font-bold text-foreground/90 truncate">{user?.name ?? "User"}</p>
          <p className="text-[10px] text-muted-foreground/50 font-medium truncate">{user?.email ?? ""}</p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="bg-border m-0" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-3 py-2 text-xs font-medium text-muted-foreground/70 hover:text-foreground cursor-pointer rounded-none"
          onClick={onOpenProfile}
        >
          <User className="h-3.5 w-3.5" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuSeparator className="bg-border m-0" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-3 py-2 text-xs font-medium text-destructive/80 hover:text-destructive hover:bg-destructive/10 cursor-pointer rounded-none"
          onClick={() => logout()}
        >
          <LogOut className="h-3.5 w-3.5" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export function WorkspaceLayout() {
  return (
    <DocumentEditorProvider>
      <WorkspaceLayoutInner />
      <Suspense fallback={null}>
        <DocumentEditorHost />
      </Suspense>
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
  const hoverTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const windowWidth = useWindowWidth();

  const contextData = location.search?.contextPanel;
  const isContextOpen = !!contextData;
  const hasSidebarContent = HIERARCHY_PAGES.has(ui.activeIcon as ContentPage);

  const [prevHasSidebarContent, setPrevHasSidebarContent] = useState(hasSidebarContent);
  const isRouteDrivenSidebarChange = hasSidebarContent !== prevHasSidebarContent;
  if (isRouteDrivenSidebarChange) {
    setPrevHasSidebarContent(hasSidebarContent);
  }

  const clearHoverTimeout = () => {
    if (hoverTimeoutRef.current) clearTimeout(hoverTimeoutRef.current);
  };
  const scheduleHoverClear = () => {
    hoverTimeoutRef.current = setTimeout(() => actions.setHoveredIcon(null), 300);
  };

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
    minWidth: 10,
    maxWidth: 500,
    direction: "left",
    onResize: (newWidth) => {
      if (newWidth === 0 && ui.isInnerSidebarOpen) {
        actions.setSidebarOpenLocal(false);
      } else if (newWidth > 0 && !ui.isInnerSidebarOpen) {
        actions.setSidebarOpenLocal(true);
      }
    },
    onResizeEnd: (newWidth) => {
      actions.updateSidebarWidth(newWidth);
    },
  });

  const currentSidebarWidth = ui.isInnerSidebarOpen ? ui.sidebarWidth : 0;
  const availableWidth = windowWidth - ICON_RAIL_WIDTH - currentSidebarWidth;
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

  const shouldShowSidebar = ui.isInnerSidebarOpen && hasSidebarContent;
  const sidebarDisplayWidth = isResizingSidebar ? sidebarWidth : ui.sidebarWidth;

  return (
    <div className="flex h-screen w-full flex-col p-1 gap-1 bg-background font-sans overflow-hidden">
      <OfflineBanner />
      {/* ═══════════════════════════════════════════════════
          HEADER BAR: Search & Global Actions
      ═══════════════════════════════════════════════════ */}
      <header className="h-9 w-full shrink-0 flex items-center justify-center relative px-1 bg-card border border-border rounded-md shadow-sm">
        <div className="absolute left-1 flex items-center gap-2">
          <WorkspaceSwitcher />
        </div>

        <GlobalSearch />

        <div className="absolute right-1 flex items-center gap-1.5">
          <NotificationBell />
          <div className="h-6 w-px bg-border/50 mx-1" />
          <UserMenu onOpenProfile={() => setProfileOpen(true)} />
        </div>
      </header>

      <div className="flex-1 flex gap-1 min-h-0 relative">
        {/* Wrap Rail and Peek Frame to maintain hover state with timeout */}
        <div className="relative h-full" onMouseEnter={clearHoverTimeout} onMouseLeave={scheduleHoverClear}>
          {/* ═══════════════════════════════════════════════════
              COLUMN 1: Icon Rail
          ═══════════════════════════════════════════════════ */}
          <IconRail onSelectIcon={handleSelectIcon} />

          {/* ─── Hover Peek Frame ───────────────────────────── */}
          {ui.hoveredIcon && HIERARCHY_PAGES.has(ui.hoveredIcon) && !shouldShowSidebar && (
            <div
              className="absolute top-0 left-[44px] h-full w-64 z-50 animate-in fade-in slide-in-from-left-1 duration-200"
              onMouseEnter={clearHoverTimeout}
              onMouseLeave={scheduleHoverClear}
            >
              <div className="h-full w-full bg-card border border-border rounded-md shadow-xl overflow-hidden">
                <HierarchySidebar />
              </div>
            </div>
          )}
        </div>

        {/* ═══════════════════════════════════════════════════
            COLUMN 2: Inner Sidebar (resizable)
        ═══════════════════════════════════════════════════ */}
        <ResizablePanel
          width={sidebarDisplayWidth}
          isResizing={isResizingSidebar}
          onResizeStart={startResizingSidebar}
          handleSide="right"
          collapsed={!shouldShowSidebar}
          animate={!isRouteDrivenSidebarChange}
        >
          {/* Inner wrapper forces fixed width so content doesn't squish during animation */}
          <div style={{ width: ui.sidebarWidth }} className="h-full flex flex-col flex-none">
            <div className="h-8 flex items-center justify-between pl-3 pr-1 shrink-0 border-b border-border bg-muted/10">
              <h2 className="font-black text-[10px] uppercase tracking-[0.15em] text-foreground/70">
                {hasSidebarContent ? "PROJECTS" : ui.activeIcon}
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

            <div className="flex-1 min-h-0 overflow-hidden">
              <HierarchySidebar />
            </div>
          </div>
        </ResizablePanel>

        {/* ═══════════════════════════════════════════════════
            COLUMN 3: Main Canvas
        ═══════════════════════════════════════════════════ */}
        <div className="flex-1 min-w-0 h-full flex flex-col relative bg-card border border-border rounded-md shadow-sm overflow-hidden">
          <Suspense fallback={<LoadingScreen label="Loading" />}>
            <Outlet />
          </Suspense>
        </div>

        {/* ═══════════════════════════════════════════════════
            COLUMN 4: Context Panel (resizable)
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

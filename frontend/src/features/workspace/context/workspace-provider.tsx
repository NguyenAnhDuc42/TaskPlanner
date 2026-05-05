import { 
  createContext, 
  useContext, 
  useEffect, 
  useState, 
  useCallback, 
  useMemo, 
  type ReactNode 
} from "react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspaceDetail, type WorkspaceSecurityContext } from "../api";
import { signalRService } from "@/lib/signalr-service";
import type { StatusDto } from "@/types/status";
import type { ContentPage } from "../type";
import { useLocalStorage } from "@/hooks/use-local-storage";

// ─── Types ───────────────────────────────────────────────

interface WorkspaceUIState {
  activeIcon: ContentPage;
  hoveredIcon: ContentPage | null;
  isInnerSidebarOpen: boolean;
  sidebarWidth: number;
  contextWidth: number;
}

interface WorkspaceActions {
  toggleInnerSidebar: () => void;
  setHoveredIcon: (icon: ContentPage | null) => void;
  updateSidebarWidth: (width: number) => void;
  updateContextWidth: (width: number) => void;
  setSidebarOpenLocal: (isOpen: boolean) => void;
  setSidebarWidthLocal: (width: number) => void;
  setContextWidthLocal: (width: number) => void;
}

interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceSecurityContext | undefined;
  statuses: StatusDto[];
  isLoading: boolean;
  isError: boolean;
  error: any;
  ui: WorkspaceUIState;
  actions: WorkspaceActions;
}

const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);

export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error("useWorkspace must be used within a WorkspaceProvider");
  }
  return context;
}

export function useWorkspaceSession() {
  const { ui: state, actions, workspaceId, isLoading } = useWorkspace();
  return { state, actions, workspaceId, isLoading };
}

// ─── Provider ────────────────────────────────────────────

interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const navigate = useNavigate();
  const location = useLocation();

  // 1. Fetch Workspace Data
  const {
    data: workspace,
    isLoading: isWorkspaceLoading,
    error,
    isError,
  } = useWorkspaceDetail(workspaceId);

  // 2. Local UI State (Managed via LocalStorage)
  const [sidebarWidth, setSidebarWidth] = useLocalStorage(`ws-${workspaceId}-sidebar-width`, 260);
  const [contextWidth, setContextWidth] = useLocalStorage(`ws-${workspaceId}-context-width`, 350);
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useLocalStorage(`ws-${workspaceId}-sidebar-open`, true);
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);

  // 3. Derive Active Icon from URL (Source of Truth)
  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    const icon = segments[3] || "projects";
    return icon as ContentPage;
  }, [location.pathname]);



  const updateSidebarWidth = useCallback((newWidth: number) => {
    setSidebarWidth(newWidth);
    setIsInnerSidebarOpen(newWidth > 0);
  }, [setSidebarWidth, setIsInnerSidebarOpen]);

  const updateContextWidth = useCallback((newWidth: number) => {
    setContextWidth(newWidth);
  }, [setContextWidth]);

  const toggleInnerSidebar = useCallback(() => {
    const nextOpen = !isInnerSidebarOpen;
    setIsInnerSidebarOpen(nextOpen);
    
    if (nextOpen && sidebarWidth < 10) {
      setSidebarWidth(260);
    }
  }, [isInnerSidebarOpen, sidebarWidth, setIsInnerSidebarOpen, setSidebarWidth]);

  // 5. Lifecycle Effects (SignalR, Redirects)
  useEffect(() => {
    if (workspace && !isError) {
      localStorage.setItem("lastWorkspaceId", workspaceId);
    }
  }, [workspaceId, workspace, isError]);

  useEffect(() => {
    if (isError && error) {
      const status = (error as any)?.response?.status;
      if (status === 403 || status === 404) {
        localStorage.removeItem("lastWorkspaceId");
        navigate({ to: "/" });
      }
    }
  }, [isError, error, navigate]);

  useEffect(() => {
    const manageConnection = async () => {
      try {
        await signalRService.startConnection();
        await signalRService.invoke("JoinWorkspace", workspaceId);
      } catch (err) {
        console.error("[SignalR] Join error:", err);
      }
    };
    manageConnection();
    return () => {
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId]);

  // 6. Memoized Context Value
  const value = useMemo(() => ({
    workspaceId,
    workspace,
    statuses: [],
    isLoading: isWorkspaceLoading,
    isError,
    error,
    ui: {
      activeIcon,
      hoveredIcon,
      isInnerSidebarOpen,
      sidebarWidth,
      contextWidth,
    },
    actions: {
      toggleInnerSidebar,
      setHoveredIcon,
      updateSidebarWidth,
      updateContextWidth,
      setSidebarOpenLocal: setIsInnerSidebarOpen,
      setSidebarWidthLocal: setSidebarWidth,
      setContextWidthLocal: setContextWidth,
    }
  }), [
    workspaceId, 
    workspace, 
    isWorkspaceLoading, 
    isError, 
    error,
    activeIcon,
    hoveredIcon,
    isInnerSidebarOpen,
    sidebarWidth,
    contextWidth,
    toggleInnerSidebar,
    updateSidebarWidth,
    updateContextWidth,
    setIsInnerSidebarOpen,
    setSidebarWidth,
    setContextWidth
  ]);

  return (
    <WorkspaceContext.Provider value={value}>
      {children}
    </WorkspaceContext.Provider>
  );
}

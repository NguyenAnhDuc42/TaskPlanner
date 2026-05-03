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
import { 
  useUserPreference, 
  useUpdateUserPreference 
} from "@/features/main/user-preference-api";
import { signalRService } from "@/lib/signalr-service";
import type { StatusDto } from "@/types/status";
import type { ContentPage } from "../type";

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
  const { data: preferences, isLoading: isPrefsLoading } = useUserPreference();
  const { mutate: updatePreferences } = useUpdateUserPreference();

  // 1. Fetch Workspace Data
  const {
    data: workspace,
    isLoading: isWorkspaceLoading,
    error,
    isError,
  } = useWorkspaceDetail(workspaceId);

  // 2. Local UI State (Initialized from preferences cache if available)
  const wsSettings = preferences?.workspaceSettings?.[workspaceId];

  const [sidebarWidth, setSidebarWidth] = useState(() => wsSettings?.sideBarWidth ?? 260);
  const [contextWidth, setContextWidth] = useState(() => wsSettings?.contextContentWidth ?? 350);
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useState(() => wsSettings?.isSidebarOpen ?? true);
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);

  // Sync preferences when they load OR when workspace switches
  useEffect(() => {
    if (!isPrefsLoading && wsSettings) {
      if (wsSettings.sideBarWidth !== undefined) setSidebarWidth(wsSettings.sideBarWidth);
      if (wsSettings.contextContentWidth !== undefined) setContextWidth(wsSettings.contextContentWidth);
      if (wsSettings.isSidebarOpen !== undefined) setIsInnerSidebarOpen(wsSettings.isSidebarOpen);
    }
  }, [workspaceId, isPrefsLoading, wsSettings]); // Now includes wsSettings for real-time sync if needed

  // 3. Derive Active Icon from URL (Source of Truth)
  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    const icon = segments[3] || "projects";
    return icon as ContentPage;
  }, [location.pathname]);

  // 4. Persistence Actions
  const isValidGuid = useCallback((id: string) => {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id);
  }, []);

  const updateSidebarWidth = useCallback((newWidth: number) => {
    setSidebarWidth(newWidth);
    const shouldBeOpen = newWidth > 0;
    setIsInnerSidebarOpen(shouldBeOpen);

    if (isValidGuid(workspaceId)) {
      updatePreferences({
        workspaceSettings: {
          [workspaceId]: {
            ...wsSettings,
            sideBarWidth: newWidth,
            isSidebarOpen: shouldBeOpen,
          }
        }
      } as any);
    }
  }, [workspaceId, wsSettings, updatePreferences, isValidGuid]);

  const updateContextWidth = useCallback((newWidth: number) => {
    setContextWidth(newWidth);
    if (isValidGuid(workspaceId)) {
      updatePreferences({
        workspaceSettings: {
          [workspaceId]: {
            ...wsSettings,
            contextContentWidth: newWidth,
          }
        }
      } as any);
    }
  }, [workspaceId, wsSettings, updatePreferences, isValidGuid]);

  const toggleInnerSidebar = useCallback(() => {
    const nextOpen = !isInnerSidebarOpen;
    setIsInnerSidebarOpen(nextOpen);
    
    let targetWidth = sidebarWidth;
    if (nextOpen && sidebarWidth < 10) {
      targetWidth = 260;
      setSidebarWidth(targetWidth);
    }

    if (isValidGuid(workspaceId)) {
      updatePreferences({
        workspaceSettings: {
          [workspaceId]: {
            ...wsSettings,
            isSidebarOpen: nextOpen,
            sideBarWidth: targetWidth
          }
        }
      } as any);
    }
  }, [workspaceId, wsSettings, isInnerSidebarOpen, sidebarWidth, updatePreferences, isValidGuid]);

  // 5. Lifecycle Effects (SignalR, Redirects)
  useEffect(() => {
    if (workspace && !isError && isValidGuid(workspaceId)) {
      updatePreferences({ lastWorkspaceId: workspaceId });
    }
  }, [workspaceId, workspace, isError, updatePreferences, isValidGuid]);

  useEffect(() => {
    if (isError && error) {
      const status = (error as any)?.response?.status;
      if (status === 403 || status === 404) {
        navigate({ to: "/" });
      }
    }
  }, [isError, error, navigate]);

  useEffect(() => {
    if (!isValidGuid(workspaceId)) return;
    
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
  }, [workspaceId, isValidGuid]);

  // 6. Memoized Context Value
  const value = useMemo(() => ({
    workspaceId,
    workspace,
    statuses: [],
    isLoading: isWorkspaceLoading || isPrefsLoading,
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
    isPrefsLoading, 
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

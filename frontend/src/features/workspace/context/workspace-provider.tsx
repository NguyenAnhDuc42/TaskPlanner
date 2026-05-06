import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useMemo,
  type ReactNode,
} from "react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import {
  useWorkspaceDetail,
  useWorkspaceWorkflows,
  useWorkspaceMembers,
  type WorkspaceSecurityContext,
} from "../api";
import { signalRService } from "@/lib/signalr-service";
import type { StatusDto } from "@/types/status";
import type { ContentPage } from "../type";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { WorkspaceContext } from "./workspace-context";
import type { 
  WorkspaceRegistry, 
  WorkspaceUIState, 
  WorkspaceUIActions,
  WorkspaceContextType 
} from "./workspace-context";


export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error("useWorkspace must be used within a WorkspaceProvider");
  }
  return context;
}

export function useWorkspaceSession() {
  const context = useContext(WorkspaceContext);
  
  // Resilient fallback to prevent "useWorkspace must be used within a WorkspaceProvider" crash
  if (!context) {
    return {
      state: {
        activeIcon: "projects",
        hoveredIcon: null,
        isInnerSidebarOpen: true,
        sidebarWidth: 260,
        contextWidth: 350,
      } as WorkspaceUIState,
      actions: {
        toggleInnerSidebar: () => {},
        setHoveredIcon: () => {},
        updateSidebarWidth: () => {},
        updateContextWidth: () => {},
        setSidebarOpenLocal: () => {},
        setSidebarWidthLocal: () => {},
        setContextWidthLocal: () => {},
      } as WorkspaceUIActions,
      workspaceId: "",
      isLoading: true,
      registry: { statusMap: {}, memberMap: {}, workflows: [] },
    };
  }

  return { 
    state: context.ui, 
    actions: context.actions, 
    workspaceId: context.workspaceId, 
    isLoading: context.isLoading, 
    registry: context.registry 
  };
}

// ─── Provider ────────────────────────────────────────────

interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({
  workspaceId,
  children,
}: WorkspaceProviderProps) {
  const navigate = useNavigate();
  const location = useLocation();

  // 1. Fetch Workspace Data
  const {
    data: workspace,
    isLoading: isWorkspaceLoading,
    error,
    isError,
  } = useWorkspaceDetail(workspaceId);

  // 2. Fetch Registry Data (Workflows & Members)
  const { data: workflows = [], isLoading: isWorkflowsLoading } =
    useWorkspaceWorkflows(workspaceId);
  const { data: memberData, isLoading: isMembersLoading } =
    useWorkspaceMembers(workspaceId);

  // 3. Build Lookup Dictionaries (Memoized)
  const registry = useMemo((): WorkspaceRegistry => {
    const statusMap: Record<string, StatusDto> = {};
    const memberMap: Record<string, any> = {};

    // Build status map from all workflows
    workflows.forEach((wf: any) => {
      wf.statuses?.forEach((status: StatusDto) => {
        statusMap[status.id] = status;
      });
    });

    // Build member map
    // Note: memberData is a PagedResult, so we look at memberData.items
    const members = (memberData as any)?.items || [];
    members.forEach((m: any) => {
      memberMap[m.workspaceMemberId] = m;
    });

    return {
      statusMap,
      memberMap,
      workflows,
    };
  }, [workflows, memberData]);

  // 4. Local UI State (Managed via LocalStorage)
  const [sidebarWidth, setSidebarWidth] = useLocalStorage(
    `ws-${workspaceId}-sidebar-width`,
    260,
  );
  const [contextWidth, setContextWidth] = useLocalStorage(
    `ws-${workspaceId}-context-width`,
    350,
  );
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useLocalStorage(
    `ws-${workspaceId}-sidebar-open`,
    true,
  );
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);

  // 5. Derive Active Icon from URL (Source of Truth)
  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    const icon = segments[3] || "projects";
    return icon as ContentPage;
  }, [location.pathname]);

  const updateSidebarWidth = useCallback(
    (newWidth: number) => {
      setSidebarWidth(newWidth);
      setIsInnerSidebarOpen(newWidth > 0);
    },
    [setSidebarWidth, setIsInnerSidebarOpen],
  );

  const updateContextWidth = useCallback(
    (newWidth: number) => {
      setContextWidth(newWidth);
    },
    [setContextWidth],
  );

  const toggleInnerSidebar = useCallback(() => {
    const nextOpen = !isInnerSidebarOpen;
    setIsInnerSidebarOpen(nextOpen);

    if (nextOpen && sidebarWidth < 10) {
      setSidebarWidth(260);
    }
  }, [
    isInnerSidebarOpen,
    sidebarWidth,
    setIsInnerSidebarOpen,
    setSidebarWidth,
  ]);

  // 6. Lifecycle Effects (SignalR, Redirects)
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

  // 7. Memoized Context Value
  const isLoading =
    isWorkspaceLoading || isWorkflowsLoading || isMembersLoading;

  const value = useMemo(
    () => ({
      workspaceId,
      workspace,
      registry,
      isLoading,
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
      },
    }),
    [
      workspaceId,
      workspace,
      registry,
      isLoading,
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
      setContextWidth,
    ],
  );

  return (
    <WorkspaceContext.Provider value={value}>
      {children}
    </WorkspaceContext.Provider>
  );
}

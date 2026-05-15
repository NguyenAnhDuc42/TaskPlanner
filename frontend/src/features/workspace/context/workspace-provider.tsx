import {
  useContext,
  useEffect,
  useCallback,
  useMemo,
  type ReactNode,
} from "react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import {
  useWorkspaceDetail,
  useWorkspaceWorkflows,
  useWorkspaceMembers,
} from "../api";
import { useWorkspaces } from "@/features/main/home-screen/api";
import { signalRService } from "@/lib/signalr-service";
import type { Status } from "@/types/status";
import type { ContentPage } from "../type";
import { WorkspaceContext } from "./workspace-context";
import type { WorkspaceRegistry } from "./workspace-context";
import { useWorkspaceUIStore } from "./use-workspace-ui-store";


export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    // Fallback to prevent crashes if used outside provider
    return {
      workspaceId: "",
      workspace: undefined,
      workspaces: [],
      isLoading: true,
      isError: false,
      error: null,
      ui: {
        activeIcon: "projects" as const,
        hoveredIcon: null,
        isInnerSidebarOpen: true,
        sidebarWidth: 260,
        contextWidth: 350,
      },
      actions: {
        toggleInnerSidebar: () => {},
        setHoveredIcon: () => {},
        updateSidebarWidth: () => {},
        updateContextWidth: () => {},
        setSidebarOpenLocal: () => {},
        setSidebarWidthLocal: () => {},
        setContextWidthLocal: () => {},
      },
      registry: { statusMap: {}, memberMap: {}, workflows: [] },
    } as any; // Cast to any to satisfy the complex return type in fallback
  }
  return context;
}

export function useWorkspaceSession() {
  const context = useContext(WorkspaceContext);

  // Resilient fallback to prevent "useWorkspace must be used within a WorkspaceProvider" crash
  if (!context) {
    return {
      state: {
        activeIcon: "projects" as const,
        hoveredIcon: null,
        isInnerSidebarOpen: true,
        sidebarWidth: 260,
        contextWidth: 350,
      },
      actions: {
        toggleInnerSidebar: () => {},
        setHoveredIcon: () => {},
        updateSidebarWidth: () => {},
        updateContextWidth: () => {},
        setSidebarOpenLocal: () => {},
        setSidebarWidthLocal: () => {},
        setContextWidthLocal: () => {},
      },
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
    registry: (context as any).registry, // Cast to any since we removed it from the type
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

  // 2.5. Fetch Workspaces List (for switcher)
  const { data: workspacesData, isLoading: isWorkspacesLoading } = useWorkspaces();
  const workspaces = useMemo(() => {
    return workspacesData?.pages.flatMap((page) => page.items) ?? [];
  }, [workspacesData]);

  // 3. Build Lookup Dictionaries (Memoized)
  const registry = useMemo((): WorkspaceRegistry => {
    const statusMap: Record<string, Status> = {};
    const memberMap: Record<string, any> = {};

    workflows.forEach((wf: any) => {
      wf.statuses?.forEach((status: Status) => {
        statusMap[status.statusId] = status;
      });
    });

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

  // 4. Local UI State (Managed via Zustand Store)
  const { getSettings, updateSettings, hoveredIcon, setHoveredIcon } = useWorkspaceUIStore();
  const settings = getSettings(workspaceId);
  
  const sidebarWidth = settings.sidebarWidth;
  const contextWidth = settings.contextWidth;
  const isInnerSidebarOpen = settings.isInnerSidebarOpen;

  // 5. Derive Active Icon from URL (Source of Truth)
  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    const icon = segments[3] || "projects";
    return icon as ContentPage;
  }, [location.pathname]);

  const updateSidebarWidth = useCallback(
    (newWidth: number) => {
      updateSettings(workspaceId, { sidebarWidth: newWidth, isInnerSidebarOpen: newWidth > 0 });
    },
    [workspaceId, updateSettings],
  );

  const updateContextWidth = useCallback(
    (newWidth: number) => {
      updateSettings(workspaceId, { contextWidth: newWidth });
    },
    [workspaceId, updateSettings],
  );

  const toggleInnerSidebar = useCallback(() => {
    const nextOpen = !isInnerSidebarOpen;
    updateSettings(workspaceId, { isInnerSidebarOpen: nextOpen });

    if (nextOpen && sidebarWidth < 10) {
      updateSettings(workspaceId, { sidebarWidth: 260 });
    }
  }, [workspaceId, isInnerSidebarOpen, sidebarWidth, updateSettings]);

  // Temporary bridges for old context actions (so we don't break layout yet)
  const setSidebarOpenLocal = useCallback((isOpen: boolean) => updateSettings(workspaceId, { isInnerSidebarOpen: isOpen }), [workspaceId, updateSettings]);
  const setSidebarWidthLocal = useCallback((width: number) => updateSettings(workspaceId, { sidebarWidth: width }), [workspaceId, updateSettings]);
  const setContextWidthLocal = useCallback((width: number) => updateSettings(workspaceId, { contextWidth: width }), [workspaceId, updateSettings]);

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
    isWorkspaceLoading || isWorkflowsLoading || isMembersLoading || isWorkspacesLoading;

  const value = useMemo(
    () => ({
      workspaceId,
      workspace,
      workspaces,
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
        setSidebarOpenLocal,
        setSidebarWidthLocal,
        setContextWidthLocal,
      },
    }),
    [
      workspaceId,
      workspace,
      workspaces,
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
      setHoveredIcon,
      updateSidebarWidth,
      updateContextWidth,
      setSidebarOpenLocal,
      setSidebarWidthLocal,
      setContextWidthLocal,
    ],
  );

  return (
    <WorkspaceContext.Provider value={value}>
      {children}
    </WorkspaceContext.Provider>
  );
}

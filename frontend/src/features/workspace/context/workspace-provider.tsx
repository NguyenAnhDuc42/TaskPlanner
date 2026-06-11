import {
  useContext,
  useEffect,
  useCallback,
  useMemo,
  type ReactNode,
} from "react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useGetWorkspaceDetailQuery } from "../api";
import type { ContentPage } from "../type";
import { WorkspaceContext } from "./workspace-context";
import { useWorkspaceUIStore } from "./use-workspace-ui-store";
import { useWorkspaceSignalR } from "./use-workspace-signalr";

export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    return {
      workspaceId: "",
      workspace: undefined,
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
    } as any;
  }
  return context;
}

export function useWorkspaceSession() {
  const context = useContext(WorkspaceContext);

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
    };
  }

  return {
    state: context.ui,
    actions: context.actions,
    workspaceId: context.workspaceId,
    isLoading: context.isLoading,
  };
}

// ─── Provider ─────────────────────────────────────────────────────────────────
// Fetches are fired by the route loader — this provider just subscribes to the
// workspace detail for permissions/role and owns the UI state for the session.

interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const navigate  = useNavigate();
  const location  = useLocation();

  // Workspace detail — role, permissions. Data already in cache from route loader.
  const { data: workspace, isLoading, error, isError } = useGetWorkspaceDetailQuery(workspaceId);

  // UI state — persisted per workspace via Zustand
  const { getSettings, updateSettings, hoveredIcon, setHoveredIcon } = useWorkspaceUIStore();
  const settings = getSettings(workspaceId);
  const { sidebarWidth, contextWidth, isInnerSidebarOpen } = settings;

  // Active icon derived from URL — never stale
  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    return (segments[3] || "projects") as ContentPage;
  }, [location.pathname]);

  const updateSidebarWidth = useCallback(
    (w: number) => updateSettings(workspaceId, { sidebarWidth: w, isInnerSidebarOpen: w > 0 }),
    [workspaceId, updateSettings],
  );

  const updateContextWidth = useCallback(
    (w: number) => updateSettings(workspaceId, { contextWidth: w }),
    [workspaceId, updateSettings],
  );

  const toggleInnerSidebar = useCallback(() => {
    const next = !isInnerSidebarOpen;
    updateSettings(workspaceId, { isInnerSidebarOpen: next });
    if (next && sidebarWidth < 10) updateSettings(workspaceId, { sidebarWidth: 260 });
  }, [workspaceId, isInnerSidebarOpen, sidebarWidth, updateSettings]);

  const setSidebarOpenLocal  = useCallback((v: boolean) => updateSettings(workspaceId, { isInnerSidebarOpen: v }), [workspaceId, updateSettings]);
  const setSidebarWidthLocal = useCallback((v: number)  => updateSettings(workspaceId, { sidebarWidth: v }),       [workspaceId, updateSettings]);
  const setContextWidthLocal = useCallback((v: number)  => updateSettings(workspaceId, { contextWidth: v }),       [workspaceId, updateSettings]);

  // Save last visited workspace + redirect on access errors
  useEffect(() => {
    if (workspace && !isError) localStorage.setItem("lastWorkspaceId", workspaceId);
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

  // Realtime — joins SignalR group, dispatches entity updates directly into Redux
  useWorkspaceSignalR(workspaceId);

  const value = useMemo(
    () => ({
      workspaceId,
      workspace,
      isLoading,
      isError,
      error,
      ui: { activeIcon, hoveredIcon, isInnerSidebarOpen, sidebarWidth, contextWidth },
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
      workspaceId, workspace, isLoading, isError, error,
      activeIcon, hoveredIcon, isInnerSidebarOpen, sidebarWidth, contextWidth,
      toggleInnerSidebar, setHoveredIcon, updateSidebarWidth, updateContextWidth,
      setSidebarOpenLocal, setSidebarWidthLocal, setContextWidthLocal,
    ],
  );

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

import {
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


interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const navigate  = useNavigate();
  const location  = useLocation();

  const { data: workspace, isLoading, error, isError } = useGetWorkspaceDetailQuery(workspaceId);

  const { sidebarWidth, contextWidth, isInnerSidebarOpen, updateSettings, hoveredIcon, setHoveredIcon } = useWorkspaceUIStore();

  const activeIcon = useMemo(() => {
    const segments = location.pathname.split("/");
    return (segments[3] || "projects") as ContentPage;
  }, [location.pathname]);

  const updateSidebarWidth = useCallback(
    (w: number) => updateSettings({ sidebarWidth: w, isInnerSidebarOpen: w > 0 }),
    [updateSettings],
  );

  const updateContextWidth = useCallback(
    (w: number) => updateSettings({ contextWidth: w }),
    [updateSettings],
  );

  const toggleInnerSidebar = useCallback(() => {
    const next = !isInnerSidebarOpen;
    updateSettings({ isInnerSidebarOpen: next });
    if (next && sidebarWidth < 10) updateSettings({ sidebarWidth: 260 });
  }, [isInnerSidebarOpen, sidebarWidth, updateSettings]);

  const setSidebarOpenLocal  = useCallback((v: boolean) => updateSettings({ isInnerSidebarOpen: v }), [updateSettings]);
  const setSidebarWidthLocal = useCallback((v: number)  => updateSettings({ sidebarWidth: v }),       [updateSettings]);
  const setContextWidthLocal = useCallback((v: number)  => updateSettings({ contextWidth: v }),       [updateSettings]);

  // Save last visited workspace + redirect on access errors
  useEffect(() => {
    if (workspace && !isError) localStorage.setItem("lastWorkspaceId", workspaceId);
  }, [workspaceId, workspace, isError]);

  useEffect(() => {
    if (isError && error) {
      const err = error as { status?: number };
      const status = err.status;
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

import {
  useEffect,
  useCallback,
  useMemo,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import { autorun } from "mobx";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import axios from "axios";
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
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const workspaceMutations = useMemo(() => new WorkspaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const workspace = useSyncExternalStore(
    (onStoreChange) => autorun(() => { rootStore.workspaceStore.getById(workspaceId); onStoreChange(); }),
    () => rootStore.workspaceStore.getById(workspaceId),
  );
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const isError = error != null;

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    workspaceMutations.fetchDetail(workspaceId)
      .catch((err) => {
        if (!cancelled) setError(err);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => { cancelled = true; };
  }, [workspaceId, workspaceMutations]);

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
      const status = axios.isAxiosError(error) ? error.response?.status : undefined;
      if (status === 403 || status === 404) {
        localStorage.removeItem("lastWorkspaceId");
      }
    }
  }, [isError, error]);

  // Realtime — joins the SignalR group for this workspace, keeps it alive across reconnects
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

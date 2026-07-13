import {
  useEffect,
  useCallback,
  useMemo,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import { autorun } from "mobx";
import { useLocation } from "@tanstack/react-router";
import { useStore, type RootStore } from "@/stores/root.store";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import { apiEvents } from "@/lib/api-client";
import { deleteWorkspaceDB } from "@/db/schema";
import axios from "axios";
import type { ContentPage } from "../type";
import { WorkspaceContext } from "./workspace-context";
import { useWorkspaceUIStore } from "./use-workspace-ui-store";
import { useWorkspaceSignalR } from "./use-workspace-signalr";


interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

function subscribeToWorkspaceRecord(rootStore: RootStore, workspaceId: string, onStoreChange: () => void) {
  return autorun(() => { void rootStore.workspaceStore.getById(workspaceId); onStoreChange(); });
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const location  = useLocation();
  const rootStore = useStore();
  const workspaceMutations = useMemo(() => new WorkspaceMutations(rootStore), [rootStore]);

  const subscribeToWorkspace = useCallback(
    (onStoreChange: () => void) => subscribeToWorkspaceRecord(rootStore, workspaceId, onStoreChange),
    [rootStore, workspaceId],
  );
  const getWorkspaceSnapshot = useCallback(
    () => rootStore.workspaceStore.getById(workspaceId),
    [rootStore, workspaceId],
  );
  const workspace = useSyncExternalStore(subscribeToWorkspace, getWorkspaceSnapshot);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const isError = error != null;

  const [prevWorkspaceId, setPrevWorkspaceId] = useState(workspaceId);
  if (workspaceId !== prevWorkspaceId) {
    setPrevWorkspaceId(workspaceId);
    setIsLoading(true);
    setError(null);
  }

  useEffect(() => {
    let cancelled = false;

    workspaceMutations.fetchDetail(workspaceId)
      .catch((err) => {
        if (!cancelled) setError(err);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => { cancelled = true; };
  }, [workspaceId, workspaceMutations]);

  useEffect(() => {
    let unmounted = false;
    const handleOnline = () => {
      setIsLoading(true);
      setError(null);
      workspaceMutations.fetchDetail(workspaceId)
        .catch((err) => {
          if (!unmounted) setError(err);
        })
        .finally(() => {
          if (!unmounted) setIsLoading(false);
        });
    };
    window.addEventListener("online", handleOnline);
    return () => {
      unmounted = true;
      window.removeEventListener("online", handleOnline);
    };
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

  useEffect(() => {
    const onRevoked = (revokedId: string) => {
      if (revokedId !== workspaceId) return;
      void rootStore.markWorkspaceAccessRevoked(revokedId);
      localStorage.removeItem("lastWorkspaceId");
      void deleteWorkspaceDB(revokedId);
    };
    apiEvents.onWorkspaceAccessRevoked.push(onRevoked);
    return () => {
      apiEvents.onWorkspaceAccessRevoked = apiEvents.onWorkspaceAccessRevoked.filter((cb) => cb !== onRevoked);
    };
  }, [workspaceId, rootStore]);

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

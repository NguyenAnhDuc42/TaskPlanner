import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { apiEvents } from "@/lib/api-client";
import { WorkspaceRootStore, WorkspaceRootStoreProvider } from "@/stores/workspace-root.store";
import { SyncEngine } from "./sync-engine";
import { devError, devLog } from "./dev-log";
import { LoadingScreen } from "@/components/loading-screen";

interface SyncEngineContextValue {
  engine: SyncEngine;
  ready: boolean;
  error: Error | null;
}

const SyncEngineContext = createContext<SyncEngineContextValue | null>(null);

export function useSyncEngine(): SyncEngine {
  const ctx = useContext(SyncEngineContext);
  if (!ctx) throw new Error("useSyncEngine must be used within SyncProvider");
  return ctx.engine;
}
export function useSyncReady(): { ready: boolean; error: Error | null } {
  const ctx = useContext(SyncEngineContext);
  if (!ctx) return { ready: false, error: null };
  return { ready: ctx.ready, error: ctx.error };
}

interface SyncProviderProps {
  workspaceId: string;
  children: ReactNode;
}

interface SyncState {
  workspaceId: string;
  workspaceRootStore: WorkspaceRootStore | null;
  syncEngine: SyncEngine | null;
  ready: boolean;
  error: Error | null;
}

export function SyncProvider({ workspaceId, children }: Readonly<SyncProviderProps>) {
  const [state, setState] = useState<SyncState>(() => ({
    workspaceId,
    workspaceRootStore: null,
    syncEngine: null,
    ready: false,
    error: null,
  }));

  if (state.workspaceId !== workspaceId) {
    setState({ workspaceId, workspaceRootStore: null, syncEngine: null, ready: false, error: null });
  }

  useEffect(() => {
    let cancelled = false;
    const workspaceRootStore = new WorkspaceRootStore(workspaceId);
    const syncEngine = new SyncEngine(workspaceRootStore);

    (async () => {
      try {
        devLog("[SyncProvider] entering workspace", workspaceId);
        await workspaceRootStore.hydrate();
        // Cleanup may have already run (StrictMode double-mount, fast workspace switch) while we
        // were awaiting. Its disconnect() is useless against work that hasn't started yet — and
        // init() bumps connectGeneration itself, which would supersede cleanup's bump and let a
        // zombie engine bootstrap + hold a live SignalR connection forever. Bail out here...
        if (cancelled) return;
        await syncEngine.init(workspaceId);
        // ...and if cancellation raced init() itself, tear down the connection it just opened.
        if (cancelled) { void syncEngine.disconnect(); return; }
        setState({ workspaceId, workspaceRootStore, syncEngine, ready: true, error: null });
      } catch (err) {
        devError("[SyncProvider] failed to initialize sync engine:", err);
        if (!cancelled) {
          const asError = err instanceof Error ? err : new Error(String(err));
          setState((s) => ({ ...s, workspaceRootStore, syncEngine, error: asError }));
        }
      }
    })();

    return () => {
      cancelled = true;
      void syncEngine.disconnect();
      workspaceRootStore.dispose();
    };
  }, [workspaceId]);

  useEffect(() => {
    const engine = state.syncEngine;
    if (!engine) return;
    const onRevoked = (revokedId: string) => {
      if (revokedId !== workspaceId) return;
      void engine.disconnect();
    };
    apiEvents.onWorkspaceAccessRevoked.push(onRevoked);
    return () => {
      apiEvents.onWorkspaceAccessRevoked = apiEvents.onWorkspaceAccessRevoked.filter((cb) => cb !== onRevoked);
    };
  }, [workspaceId, state.syncEngine]);

  const contextValue = useMemo(
    () => (state.syncEngine ? { engine: state.syncEngine, ready: state.ready, error: state.error } : null),
    [state.syncEngine, state.ready, state.error],
  );

  if (state.error) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-background text-destructive text-sm">
        Failed to load this workspace. Try refreshing the page.
      </div>
    );
  }

  if (!state.ready || !state.workspaceRootStore || !contextValue) {
    return <LoadingScreen fullScreen label="Loading workspace" />;
  }

  return (
    <WorkspaceRootStoreProvider value={state.workspaceRootStore}>
      <SyncEngineContext.Provider value={contextValue}>
        {children}
      </SyncEngineContext.Provider>
    </WorkspaceRootStoreProvider>
  );
}

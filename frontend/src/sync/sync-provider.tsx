import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { RootStore, RootStoreProvider } from "@/stores/root.store";
import { SyncEngine } from "./sync-engine";
import { devError, devLog } from "./dev-log";

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

// True once bootstrap/connect has completed for the current workspace — task/space/folder/status
// stores are safe to read. Components outside the sync-engine migration (Redux-backed) don't need
// this at all; it only matters for code reading from `useStore()`.
export function useSyncReady(): { ready: boolean; error: Error | null } {
  const ctx = useContext(SyncEngineContext);
  if (!ctx) return { ready: false, error: null };
  return { ready: ctx.ready, error: ctx.error };
}

interface SyncProviderProps {
  workspaceId: string;
  children: ReactNode;
}

// Mounts the offline-first sync engine (RootStore + SyncEngine) for the given workspace —
// the real-app equivalent of what /dev/sync-test constructs standalone. Bootstraps/connects
// once per workspaceId change. Children render immediately (Redux-backed content doesn't wait
// on this at all) — components that read from `useStore()`/`useTaskStore()` etc. should check
// `useSyncReady()` themselves before trusting the store has data.
export function SyncProvider({ workspaceId, children }: Readonly<SyncProviderProps>) {
  const rootStore = useMemo(() => new RootStore(), []);
  const syncEngine = useMemo(() => new SyncEngine(rootStore), [rootStore]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let cancelled = false;
    setReady(false);
    setError(null);

    (async () => {
      try {
        devLog("[SyncProvider] switching to workspace", workspaceId);
        await rootStore.switchWorkspace(workspaceId);
        await syncEngine.init(workspaceId);
        if (!cancelled) setReady(true);
      } catch (err) {
        devError("[SyncProvider] failed to initialize sync engine:", err);
        if (!cancelled) setError(err instanceof Error ? err : new Error(String(err)));
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [workspaceId, rootStore, syncEngine]);

  const contextValue = useMemo(() => ({ engine: syncEngine, ready, error }), [syncEngine, ready, error]);

  return (
    <RootStoreProvider value={rootStore}>
      <SyncEngineContext.Provider value={contextValue}>
        {children}
      </SyncEngineContext.Provider>
    </RootStoreProvider>
  );
}

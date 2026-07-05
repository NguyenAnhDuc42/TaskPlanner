import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useStore } from "@/stores/root.store";
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
export function useSyncReady(): { ready: boolean; error: Error | null } {
  const ctx = useContext(SyncEngineContext);
  if (!ctx) return { ready: false, error: null };
  return { ready: ctx.ready, error: ctx.error };
}

interface SyncProviderProps {
  workspaceId: string;
  children: ReactNode;
}


export function SyncProvider({ workspaceId, children }: Readonly<SyncProviderProps>) {
  const rootStore = useStore();
  const syncEngine = useMemo(() => new SyncEngine(rootStore), [rootStore]);
  const [state, setState] = useState<{ workspaceId: string; ready: boolean; error: Error | null }>(
    () => ({ workspaceId, ready: false, error: null }),
  );

  if (state.workspaceId !== workspaceId) {
    setState({ workspaceId, ready: false, error: null });
  }

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        devLog("[SyncProvider] switching to workspace", workspaceId);
        await rootStore.switchWorkspace(workspaceId);
        await syncEngine.init(workspaceId);
        if (!cancelled) setState((s) => ({ ...s, ready: true }));
      } catch (err) {
        devError("[SyncProvider] failed to initialize sync engine:", err);
        if (!cancelled) {
          const asError = err instanceof Error ? err : new Error(String(err));
          setState((s) => ({ ...s, error: asError }));
        }
      }
    })();

    return () => {
      cancelled = true;
      void syncEngine.disconnect();
    };
  }, [workspaceId, rootStore, syncEngine]);

  const contextValue = useMemo(
    () => ({ engine: syncEngine, ready: state.ready, error: state.error }),
    [syncEngine, state.ready, state.error],
  );

  return (
    <SyncEngineContext.Provider value={contextValue}>
      {children}
    </SyncEngineContext.Provider>
  );
}

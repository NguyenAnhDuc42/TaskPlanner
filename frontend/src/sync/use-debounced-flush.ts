import { useCallback, useEffect, useRef } from "react";
import type { SyncEngine } from "./sync-engine";

// Shared debounce trigger for syncEngine.flushQueue(). Flush is global (not scoped to one
// entity) and already runs TransactionQueue.squash() before sending, which merges multiple
// pending updates for the SAME entity into one network call — so callers should call
// updateLocal()-style methods immediately on every change (instant optimistic UI, cheap local
// writes) and debounce this flush trigger instead of debouncing the update itself or
// hand-rolling their own patch-merging.
export function useDebouncedFlush(syncEngine: SyncEngine, delay = 1500) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleFlush = useCallback(() => {
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      timerRef.current = null;
      syncEngine.flushQueue().catch((err) => console.error("[useDebouncedFlush] flush failed:", err));
    }, delay);
  }, [syncEngine, delay]);

  const flushNow = useCallback(() => {
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = null;
    return syncEngine.flushQueue();
  }, [syncEngine]);

  // Flush whatever's pending on unmount rather than dropping it on the floor.
  useEffect(() => () => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      syncEngine.flushQueue().catch((err) => console.error("[useDebouncedFlush] flush-on-unmount failed:", err));
    }
  }, [syncEngine]);

  return { scheduleFlush, flushNow };
}

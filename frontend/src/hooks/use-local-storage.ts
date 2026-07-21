import { useCallback, useEffect, useState } from "react";

// Native "storage" events only fire in *other* tabs/windows, never same-window — so with plain
// useState, two components mounted at once (e.g. a settings dialog and the view it configures)
// each get their own isolated copy seeded once at mount. One updates its copy + localStorage; the
// other never hears about it until it remounts (a refresh). This event is how same-window
// instances of the same key notify each other.
// Exported so callers that must write localStorage directly (e.g. setting a key before a route
// navigation, outside any component's state) can still notify other same-window instances.
export const LOCAL_STORAGE_EVENT = "local-storage-change";

export function broadcastLocalStorageChange(key: string) {
  if (globalThis.window === undefined) return;
  globalThis.window.dispatchEvent(new CustomEvent(LOCAL_STORAGE_EVENT, { detail: { key } }));
}

function readValue<T>(key: string, initialValue: T): T {
  if (globalThis.window === undefined) {
    return initialValue;
  }

  try {
    const item = globalThis.window.localStorage.getItem(key);
    return item ? (JSON.parse(item) as T) : initialValue;
  } catch (error) {
    console.warn(`Error reading localStorage key “${key}”:`, error);
    return initialValue;
  }
}

export function useLocalStorage<T>(key: string, initialValue: T): [T, (value: T) => void] {
  const [storedValue, setStoredValue] = useState<T>(() => readValue(key, initialValue));

  useEffect(() => {
    if (globalThis.window === undefined) return;
    const handler = (e: Event) => {
      const changedKey = (e as CustomEvent<{ key: string }>).detail?.key;
      if (changedKey === key) {
        setStoredValue(readValue(key, initialValue));
      }
    };
    globalThis.window.addEventListener(LOCAL_STORAGE_EVENT, handler);
    return () => globalThis.window.removeEventListener(LOCAL_STORAGE_EVENT, handler);
    // initialValue intentionally excluded — callers often pass a fresh literal (`{}`/`[]`) every
    // render, which would re-subscribe this effect every render for no reason. It's only used as
    // the absent-key fallback, and the key existing/not existing doesn't change across a render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const setValue = useCallback(
    (value: T) => {
      try {
        setStoredValue(value);
        if (globalThis.window !== undefined) {
          globalThis.window.localStorage.setItem(key, JSON.stringify(value));
          globalThis.window.dispatchEvent(new CustomEvent(LOCAL_STORAGE_EVENT, { detail: { key } }));
        }
      } catch (error) {
        console.warn(`Error setting localStorage key “${key}”:`, error);
      }
    },
    [key],
  );

  return [storedValue, setValue];
}

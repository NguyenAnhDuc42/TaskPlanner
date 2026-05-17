import { createContext, useContext, useCallback, useRef, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import { useSpaceDetail, useUpdateSpace } from "./space-api";
import type { EnrichedSpaceDetailDto, UpdateSpaceRequest } from "./space-types";

interface SpaceEditorContextType {
  space: EnrichedSpaceDetailDto;
  updateField: (updates: Omit<UpdateSpaceRequest, "spaceId">) => void;
  isSaving: boolean;
}

const SpaceEditorContext = createContext<SpaceEditorContextType | null>(null);

interface SpaceEditorProviderProps {
  workspaceId: string;
  spaceId: string;
  children: React.ReactNode;
}

export function SpaceEditorProvider({ workspaceId, spaceId, children }: SpaceEditorProviderProps) {
  const { data: space, isLoading, isError } = useSpaceDetail(workspaceId, spaceId);
  const queryClient = useQueryClient();
  const updateSpace = useUpdateSpace();
  const isSaving = updateSpace.isPending;

  const pendingUpdatesRef = useRef<Omit<UpdateSpaceRequest, "spaceId">>({});
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Helper to commit any pending changes instantly
  const flushPendingUpdates = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    const finalUpdates = pendingUpdatesRef.current;
    if (Object.keys(finalUpdates).length > 0) {
      pendingUpdatesRef.current = {};
      updateSpace.mutate({ spaceId, ...finalUpdates });
    }
  }, [spaceId, updateSpace]);

  // Flush pending updates when the component unmounts OR when spaceId changes!
  useEffect(() => {
    return () => {
      flushPendingUpdates();
    };
  }, [flushPendingUpdates]);

  const updateField = useCallback(
    (updates: Omit<UpdateSpaceRequest, "spaceId">) => {
      if (!spaceId) return;

      // 1. Instantly write to the React Query cache so the UI updates with 0ms perceived lag
      queryClient.setQueryData(
        [...workspaceKeys.all, "space", spaceId],
        (old: any) => {
          if (!old) return old;
          return {
            ...old,
            ...updates
          };
        }
      );

      // 2. Coalesce/merge the updates
      pendingUpdatesRef.current = {
        ...pendingUpdatesRef.current,
        ...updates
      };

      // 3. Debounce the mutation trigger to prevent network spam
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }

      timerRef.current = setTimeout(() => {
        const finalUpdates = pendingUpdatesRef.current;
        pendingUpdatesRef.current = {};
        updateSpace.mutate({ spaceId, ...finalUpdates });
      }, 1000); 
    },
    [spaceId, queryClient, updateSpace]
  );

  if (isLoading && !space) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest bg-background h-full">
        Loading space...
      </div>
    );
  }
  
  if (isError) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-destructive/80 uppercase tracking-widest bg-background h-full">
        Failed to load space
      </div>
    );
  }
  
  if (!space) return null;

  return (
    <SpaceEditorContext.Provider value={{ space, updateField, isSaving }}>
      {children}
    </SpaceEditorContext.Provider>
  );
}

export function useSpaceEditor() {
  const context = useContext(SpaceEditorContext);
  if (!context) {
    throw new Error("useSpaceEditor must be used within a SpaceEditorProvider");
  }
  return context;
}

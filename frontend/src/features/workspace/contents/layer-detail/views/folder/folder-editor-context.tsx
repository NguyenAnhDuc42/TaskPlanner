import { createContext, useContext, useCallback, useRef, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import { useFolderDetail, useUpdateFolder } from "./folder-api";
import type { EnrichedFolderDetailDto, UpdateFolderRequest } from "./folder-types";

interface FolderEditorContextType {
  folder: EnrichedFolderDetailDto;
  updateField: (updates: Omit<UpdateFolderRequest, "folderId">) => void;
  isSaving: boolean;
}

const FolderEditorContext = createContext<FolderEditorContextType | null>(null);

interface FolderEditorProviderProps {
  workspaceId: string;
  folderId: string;
  children: React.ReactNode;
}

export function FolderEditorProvider({ workspaceId, folderId, children }: FolderEditorProviderProps) {
  const { data: folder, isLoading, isError } = useFolderDetail(workspaceId, folderId);
  const queryClient = useQueryClient();
  const updateFolder = useUpdateFolder();
  const isSaving = updateFolder.isPending;

  const pendingUpdatesRef = useRef<Omit<UpdateFolderRequest, "folderId">>({});
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
      updateFolder.mutate({ folderId, ...finalUpdates });
    }
  }, [folderId, updateFolder]);

  // Flush pending updates when the component unmounts OR when folderId changes!
  useEffect(() => {
    return () => {
      flushPendingUpdates();
    };
  }, [flushPendingUpdates]);

  const updateField = useCallback(
    (updates: Omit<UpdateFolderRequest, "folderId">) => {
      if (!folderId) return;

      // 1. Instantly write to the React Query cache so the UI updates with 0ms perceived lag
      queryClient.setQueryData(
        [...workspaceKeys.all, "folder", folderId],
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
        updateFolder.mutate({ folderId, ...finalUpdates });
      }, 1000); 
    },
    [folderId, queryClient, updateFolder]
  );

  if (isLoading && !folder) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest bg-background h-full">
        Loading folder...
      </div>
    );
  }
  
  if (isError) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-destructive/80 uppercase tracking-widest bg-background h-full">
        Failed to load folder
      </div>
    );
  }
  
  if (!folder) return null;

  return (
    <FolderEditorContext.Provider value={{ folder, updateField, isSaving }}>
      {children}
    </FolderEditorContext.Provider>
  );
}

export function useFolderEditor() {
  const context = useContext(FolderEditorContext);
  if (!context) {
    throw new Error("useFolderEditor must be used within a FolderEditorProvider");
  }
  return context;
}

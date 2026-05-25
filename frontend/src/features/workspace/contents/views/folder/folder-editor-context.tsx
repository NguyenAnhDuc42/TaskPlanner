import React, { createContext, useContext, useCallback, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type { UpdateFolderRequest } from "../../layer-detail/views/folder/folder-types";
import { useMutation } from "@tanstack/react-query";
import { api } from "@/lib/api-client";

interface FolderEditorContextValue {
  updateField: (updates: Partial<UpdateFolderRequest>) => void;
  isSaving: boolean;
}

const FolderEditorContext = createContext<FolderEditorContextValue | null>(null);

export function useFolderEditor() {
  const ctx = useContext(FolderEditorContext);
  if (!ctx) throw new Error("useFolderEditor must be used within FolderEditorProvider");
  return ctx;
}

export function FolderEditorProvider({ folderId, children }: { folderId: string, children: React.ReactNode }) {
  const queryClient = useQueryClient();
  const pendingUpdatesRef = useRef<Partial<UpdateFolderRequest>>({});
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // We define the mutation inline or import it
  const updateFolderMut = useMutation({
    mutationFn: async (req: { folderId: string } & Partial<UpdateFolderRequest>) => {
      await api.put(`/folders/${req.folderId}`, req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folderDetail", folderId] });
    }
  });

  const updateField = useCallback(
    (updates: Partial<UpdateFolderRequest>) => {
      if (!folderId) return;

      // Optimistic update
      queryClient.setQueryData(["folderDetail", folderId], (old: any) => {
        if (!old) return old;
        return {
          ...old,
          folder: {
            ...old.folder,
            ...updates
          }
        };
      });

      pendingUpdatesRef.current = {
        ...pendingUpdatesRef.current,
        ...updates
      };

      if (timerRef.current) clearTimeout(timerRef.current);

      timerRef.current = setTimeout(() => {
        const finalUpdates = pendingUpdatesRef.current;
        pendingUpdatesRef.current = {};
        updateFolderMut.mutate({ folderId, ...finalUpdates });
      }, 1000);
    },
    [folderId, queryClient, updateFolderMut]
  );

  return (
    <FolderEditorContext.Provider value={{ updateField, isSaving: updateFolderMut.isPending }}>
      {children}
    </FolderEditorContext.Provider>
  );
}

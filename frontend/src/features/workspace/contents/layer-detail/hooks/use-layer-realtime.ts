import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { hierarchyKeys } from "../../hierarchy/hierarchy-keys";
import { workspaceKeys } from "@/features/main/query-keys";

export function useLayerRealtime(workspaceId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!workspaceId) return;

    // Helper to update an entity in any node list (Folders or Tasks)
    const updateInNodeLists = (entityId: string, updates: any, type: "folders" | "tasks") => {
      queryClient.setQueriesData({ queryKey: hierarchyKeys.nodeBase(workspaceId) }, (old: any) => {
        if (!old) return old;
        
        // Handle InfiniteQuery data structure (for tasks)
        if (type === "tasks" && old.pages) {
          return {
            ...old,
            pages: old.pages.map((page: any) => ({
              ...page,
              items: page.items?.map((item: any) => 
                item.id === entityId ? { ...item, ...updates } : item
              )
            }))
          };
        }

        // Handle regular Array data structure (for folders)
        if (type === "folders" && Array.isArray(old)) {
          return old.map((item: any) => 
            item.id === entityId ? { ...item, ...updates } : item
          );
        }

        return old;
      });
    };

    const onSpaceUpdated = (data: any) => {
      const id = data.spaceId || data.id;
      queryClient.setQueryData([...workspaceKeys.all, "space", id], (old: any) => old ? { ...old, ...data } : data);
      
      // Update in Root Hierarchy (Spaces)
      queryClient.setQueryData(hierarchyKeys.detail(workspaceId), (old: any) => {
        if (!old?.spaces) return old;
        return {
          ...old,
          spaces: old.spaces.map((s: any) => s.id === id ? { ...s, ...data } : s)
        };
      });
    };

    const onFolderUpdated = (data: any) => {
      const id = data.folderId || data.id;
      queryClient.setQueryData([...workspaceKeys.all, "folder", id], (old: any) => old ? { ...old, ...data } : data);
      
      // Update in any expanded node list
      updateInNodeLists(id, data, "folders");
    };

    const onTaskUpdated = (data: any) => {
      const id = data.taskId || data.id;
      queryClient.setQueryData([...workspaceKeys.all, "task", id], (old: any) => old ? { ...old, ...data } : data);
      
      // Update in any expanded task list
      updateInNodeLists(id, data, "tasks");
    };

    const onHierarchyChanged = () => {
      // Structural changes (moves, deletes, adds) still need a refresh
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeBase(workspaceId) });
    };

    // Register Listeners
    signalRService.on("SpaceUpdated", onSpaceUpdated);
    signalRService.on("FolderUpdated", onFolderUpdated);
    signalRService.on("TaskUpdated", onTaskUpdated);
    signalRService.on("HierarchyChanged", onHierarchyChanged);

    return () => {
      signalRService.off("SpaceUpdated", onSpaceUpdated);
      signalRService.off("FolderUpdated", onFolderUpdated);
      signalRService.off("TaskUpdated", onTaskUpdated);
      signalRService.off("HierarchyChanged", onHierarchyChanged);
    };
  }, [workspaceId, queryClient]);
}

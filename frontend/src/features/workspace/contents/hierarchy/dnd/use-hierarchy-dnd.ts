import { 
  useSensor, 
  useSensors, 
  PointerSensor, 
  KeyboardSensor,
  type DragEndEvent,
  type DragStartEvent
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates, arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType, EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { 
  SpaceHierarchy, 
  WorkspaceHierarchy, 
  MoveItemRequest, 
  FolderHierarchy, 
  TaskHierarchy,
} from "../hierarchy-type";
import { useState, useRef, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { hierarchyKeys } from "../hierarchy-keys";
import { fractionalBetween } from "../utils/fractional-index";

interface UseHierarchyDndProps {
  workspaceId: string;
  filteredHierarchy: WorkspaceHierarchy | undefined;
  setLocalHierarchy: React.Dispatch<React.SetStateAction<WorkspaceHierarchy | undefined>>;
  moveItem: {
    mutate: (data: MoveItemRequest) => void;
  };
}

export function useHierarchyDnd({ workspaceId, filteredHierarchy, setLocalHierarchy, moveItem }: UseHierarchyDndProps) {
  const queryClient = useQueryClient();
  const [activeItem, setActiveItem] = useState<{ 
    id: string, 
    type: EntityLayerType, 
    data: SpaceHierarchy | FolderHierarchy | TaskHierarchy | any
  } | null>(null);

  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingMutationRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      if (pendingMutationRef.current) {
        pendingMutationRef.current();
      }
    };
  }, []);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragStart = (event: DragStartEvent) => {
    const data = event.active.data.current;
    if (!data) return;

    setActiveItem({
      id: event.active.id as string,
      type: data.type as EntityLayerType,
      data: data as any
    });
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveItem(null);
    
    if (!over || active.id === over.id) return;

    const activeData = active.data.current;
    const overData = over.data.current;

    if (!activeData || !overData) return;

    const itemType = activeData.type as EntityLayerType;

    let prevKey: string | undefined;
    let nextKey: string | undefined;
    let newOrderKey: string | undefined;

    // 1. REORDERING SPACES
    if (itemType === EntityLayerConst.ProjectSpace) {
      if (overData?.type !== EntityLayerConst.ProjectSpace) return;

      await queryClient.cancelQueries({ queryKey: hierarchyKeys.detail(workspaceId) });

      const spaces = filteredHierarchy?.spaces || [];
      const oldIndex = spaces.findIndex(s => s.id === activeData.id);
      const newIndex = spaces.findIndex(s => s.id === overData.id);
      
      if (oldIndex === -1 || newIndex === -1) return;

      const moved = arrayMove(spaces, oldIndex, newIndex);
      prevKey = newIndex > 0 ? moved[newIndex - 1]?.orderKey : undefined;
      nextKey = newIndex < moved.length - 1 ? moved[newIndex + 1]?.orderKey : undefined;
      newOrderKey = fractionalBetween(prevKey, nextKey);
      
      setLocalHierarchy((prev) => prev ? { ...prev, spaces: moved } : prev);

      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      
      const persist = () => {
        moveItem.mutate({
          itemId: activeData.id,
          itemType: EntityLayerConst.ProjectSpace,
          previousItemOrderKey: prevKey,
          nextItemOrderKey: nextKey,
          newOrderKey
        });
        pendingMutationRef.current = null;
      };

      pendingMutationRef.current = persist;
      timeoutRef.current = setTimeout(persist, 1000);
    } 
    // 2. MOVING/REORDERING FOLDERS
    else if (itemType === EntityLayerConst.ProjectFolder) {
      let targetSpaceId: string | undefined;

      if (overData?.type === EntityLayerConst.ProjectSpace) {
        targetSpaceId = overData.id;
      } else if (overData?.type === EntityLayerConst.ProjectFolder) {
        targetSpaceId = overData.parentId;
      }

      if (!targetSpaceId) return;

      const foldersKey = hierarchyKeys.nodeFolders(workspaceId, targetSpaceId);
      const folders = queryClient.getQueryData<FolderHierarchy[]>(foldersKey) || [];

      const oldIndex = folders.findIndex(f => f.id === activeData.id);
      const newIndex = folders.findIndex(f => f.id === overData.id);

      let moved = [...folders];
      if (oldIndex !== -1) {
        moved = arrayMove(folders, oldIndex, newIndex === -1 ? 0 : newIndex);
        const finalIdx = newIndex === -1 ? 0 : newIndex;
        prevKey = finalIdx > 0 ? moved[finalIdx - 1]?.orderKey : undefined;
        nextKey = finalIdx < moved.length - 1 ? moved[finalIdx + 1]?.orderKey : undefined;
      } else {
        const finalIdx = newIndex === -1 ? 0 : newIndex;
        prevKey = finalIdx > 0 ? folders[finalIdx - 1]?.orderKey : undefined;
        nextKey = finalIdx < folders.length ? folders[finalIdx]?.orderKey : undefined;
      }

      newOrderKey = fractionalBetween(prevKey, nextKey);
      const sourceSpaceId = activeData.parentId;
      const sourceKey = hierarchyKeys.nodeFolders(workspaceId, sourceSpaceId);
      const targetKey = hierarchyKeys.nodeFolders(workspaceId, targetSpaceId);

      await queryClient.cancelQueries({ queryKey: sourceKey });
      await queryClient.cancelQueries({ queryKey: targetKey });

      if (sourceSpaceId === targetSpaceId) {
        // Reordering in the same list
        queryClient.setQueryData(sourceKey, (old: any) => {
          if (!old) return old;
          const oldIndex = old.findIndex((f: any) => f.id === activeData.id);
          const newIndex = old.findIndex((f: any) => f.id === overData.id);
          
          if (oldIndex === -1 || newIndex === -1) return old;
          
          const moved = arrayMove(old, oldIndex, newIndex);
          return moved;
        });
      } else {
        // Moving across spaces
        // Remove from source
        queryClient.setQueryData(sourceKey, (old: any) => {
          if (!old) return old;
          return old.filter((f: any) => f.id !== activeData.id);
        });
        
        // Add to target
        queryClient.setQueryData(targetKey, (old: any) => {
          const data = old || [];
          return [activeData as FolderHierarchy, ...data];
        });
      }

      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      
      const persist = () => {
        moveItem.mutate({
          itemId: activeData.id,
          itemType: EntityLayerConst.ProjectFolder,
          targetParentId: targetSpaceId,
          previousItemOrderKey: prevKey,
          nextItemOrderKey: nextKey,
          newOrderKey
        });
        pendingMutationRef.current = null;
      };

      pendingMutationRef.current = persist;
      timeoutRef.current = setTimeout(persist, 1000);
    }
    // 3. MOVING/REORDERING TASKS
    else if (itemType === EntityLayerConst.ProjectTask) {
      let targetParentId: string | undefined;
      let targetParentType: EntityLayerType | undefined;

      if (overData?.type === EntityLayerConst.ProjectSpace) {
        targetParentId = overData.id;
        targetParentType = EntityLayerConst.ProjectSpace;
      } else if (overData?.type === EntityLayerConst.ProjectFolder) {
        targetParentId = overData.id;
        targetParentType = EntityLayerConst.ProjectFolder;
      } else if (overData?.type === EntityLayerConst.ProjectTask) {
        targetParentId = overData.parentId;
        targetParentType = overData.parentType;
      }

      if (!targetParentId || !targetParentType) return;

      const sourceParentId = activeData.parentId;
      const sourceKey = hierarchyKeys.nodeTasks(workspaceId, sourceParentId);
      const targetKey = hierarchyKeys.nodeTasks(workspaceId, targetParentId);

      await queryClient.cancelQueries({ queryKey: sourceKey });
      await queryClient.cancelQueries({ queryKey: targetKey });

      // Optimistic update for React Query cache (Infinite Query)
      const updateCache = (key: any, remove: boolean, add: boolean, taskData: any) => {
        queryClient.setQueryData(key, (old: any) => {
          const data = old || { pages: [{ tasks: [], hasMore: false }], pageParams: [] };
          const newPages = data.pages.map((page: any) => {
            let tasks = [...page.tasks];
            if (remove) {
              tasks = tasks.filter((t: any) => t.id !== taskData.id);
            }
            if (add && page === data.pages[0]) {
              // Add to the top of the first page for simplicity
              tasks = [taskData, ...tasks];
            }
            return { ...page, tasks };
          });
          return { ...data, pages: newPages };
        });
      };

      if (sourceParentId === targetParentId) {
        // Reordering in the same list
        queryClient.setQueryData(sourceKey, (old: any) => {
          if (!old) return old;
          const allTasks = old.pages.flatMap((p: any) => p.tasks) as TaskHierarchy[];
          const oldIndex = allTasks.findIndex((t: any) => t.id === activeData.id);
          const newIndex = allTasks.findIndex((t: any) => t.id === overData.id);
          
          if (oldIndex === -1 || newIndex === -1) return old;
          
          const moved = arrayMove(allTasks, oldIndex, newIndex);
          const finalIdx = newIndex;
          prevKey = finalIdx > 0 ? moved[finalIdx - 1]?.orderKey : undefined;
          nextKey = finalIdx < moved.length - 1 ? moved[finalIdx + 1]?.orderKey : undefined;
          newOrderKey = fractionalBetween(prevKey, nextKey);

          // Put back into pages structure
          let pointer = 0;
          const newPages = old.pages.map((page: any) => {
            const pageTasks = moved.slice(pointer, pointer + page.tasks.length);
            pointer += page.tasks.length;
            return { ...page, tasks: pageTasks };
          });
          return { ...old, pages: newPages };
        });
      } else {
        // Moving across layers
        updateCache(sourceKey, true, false, activeData);
        updateCache(targetKey, false, true, activeData);
        
        // For moving to a new list, we just put it at the top, so nextKey is the first item's key
        const targetTasks = queryClient.getQueryData<any>(targetKey);
        const firstTask = targetTasks?.pages[0]?.tasks[0];
        nextKey = firstTask?.orderKey;
        newOrderKey = fractionalBetween(undefined, nextKey);
      }

      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      
      const persist = () => {
        moveItem.mutate({
          itemId: activeData.id,
          itemType: EntityLayerConst.ProjectTask,
          targetParentId: targetParentId,
          previousItemOrderKey: prevKey,
          nextItemOrderKey: nextKey,
          newOrderKey
        });
        pendingMutationRef.current = null;
      };

      pendingMutationRef.current = persist;
      timeoutRef.current = setTimeout(persist, 1000);
    }
  };

  return {
    sensors,
    handleDragStart,
    handleDragEnd,
    activeItem
  };
}

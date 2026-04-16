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
  TaskHierarchy 
} from "../hierarchy-type";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { hierarchyKeys } from "../hierarchy-keys";
import { fractionalAfter, fractionalBetween } from "../utils/fractional-index";

interface UseHierarchyDndProps {
  workspaceId: string;
  filteredHierarchy: WorkspaceHierarchy | undefined;
  moveItem: {
    mutate: (data: MoveItemRequest) => void;
  };
}

export function useHierarchyDnd({ workspaceId, filteredHierarchy, moveItem }: UseHierarchyDndProps) {
  const queryClient = useQueryClient();
  const [activeItem, setActiveItem] = useState<{ 
    id: string, 
    type: EntityLayerType, 
    data: SpaceHierarchy | FolderHierarchy | TaskHierarchy | any
  } | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 }, // Sharp, instant drag start
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

  const handleDragEnd = (event: DragEndEvent) => {
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
    let targetParentId: string | undefined;

    // 1. REORDERING SPACES
    if (itemType === EntityLayerConst.ProjectSpace) {
      if (overData?.type !== EntityLayerConst.ProjectSpace) return;

      const spaces = filteredHierarchy?.spaces || [];
      const oldIndex = spaces.findIndex(s => s.id === activeData.id);
      const newIndex = spaces.findIndex(s => s.id === overData.id);
      
      if (oldIndex === -1 || newIndex === -1) return;

      const moved = arrayMove(spaces, oldIndex, newIndex);
      prevKey = newIndex > 0 ? moved[newIndex - 1]?.orderKey : undefined;
      nextKey = newIndex < moved.length - 1 ? moved[newIndex + 1]?.orderKey : undefined;
      newOrderKey = fractionalBetween(prevKey, nextKey);
      
      moveItem.mutate({
        itemId: activeData.id,
        itemType: EntityLayerConst.ProjectSpace,
        previousItemOrderKey: prevKey,
        nextItemOrderKey: nextKey,
        newOrderKey
      });
    } 
    // 2. MOVING/REORDERING FOLDERS
    else if (itemType === EntityLayerConst.ProjectFolder) {
      let targetSpaceId: string | undefined;

      if (overData?.type === EntityLayerConst.ProjectSpace) {
        targetSpaceId = overData.id;
      } else if (overData?.type === EntityLayerConst.ProjectFolder) {
        targetSpaceId = overData.parentId;
      } else if (overData?.type === EntityLayerConst.ProjectTask) {
        const targetParent = overData.parentType === EntityLayerConst.ProjectSpace 
          ? overData.parentId 
          : filteredHierarchy?.spaces.find(s => s.folders.some(f => f.id === overData.parentId))?.id;
        targetSpaceId = targetParent;
      }

      if (!targetSpaceId) return;
      targetParentId = targetSpaceId;

      const folders = queryClient.getQueryData<FolderHierarchy[]>(
        [...hierarchyKeys.detail(workspaceId), targetParentId, "folders"]
      ) || filteredHierarchy?.spaces.find(s => s.id === targetSpaceId)?.folders || [];

      const oldIndex = folders.findIndex(f => f.id === activeData.id);
      const newIndex = folders.findIndex(f => f.id === overData.id);

      if (oldIndex !== -1) {
        const moved = arrayMove(folders, oldIndex, newIndex === -1 ? 0 : newIndex);
        const finalIdx = newIndex === -1 ? 0 : newIndex;
        prevKey = finalIdx > 0 ? moved[finalIdx - 1]?.orderKey : undefined;
        nextKey = finalIdx < moved.length - 1 ? moved[finalIdx + 1]?.orderKey : undefined;
      } else {
        const finalIdx = newIndex === -1 ? 0 : newIndex;
        prevKey = finalIdx > 0 ? folders[finalIdx - 1]?.orderKey : undefined;
        nextKey = finalIdx < folders.length ? folders[finalIdx]?.orderKey : undefined;
      }

      newOrderKey = fractionalBetween(prevKey, nextKey);

      moveItem.mutate({
        itemId: activeData.id,
        itemType: EntityLayerConst.ProjectFolder,
        targetParentId,
        previousItemOrderKey: prevKey,
        nextItemOrderKey: nextKey,
        newOrderKey
      });
    }
    // 3. MOVING/REORDERING TASKS
    else if (itemType === EntityLayerConst.ProjectTask) {
      let targetId: string | undefined;
      
      if (overData?.type === EntityLayerConst.ProjectTask) {
        targetId = overData.parentId;
      } else if (overData?.type === EntityLayerConst.ProjectFolder || overData?.type === EntityLayerConst.ProjectSpace) {
        targetId = overData.id;
      }

      if (!targetId) targetId = activeData.parentId; 
      targetParentId = targetId;

      if (targetId) {
        // Use query cache for specific index boundaries to support UP and DOWN dragging directions
        const tasksResponse = queryClient.getQueryData<any>(
          hierarchyKeys.nodeTasks(workspaceId, targetId)
        );
        const tasks: TaskHierarchy[] = tasksResponse?.pages.flatMap((p: any) => p.tasks || []) || [];
        
        const oldIndex = tasks.findIndex((t: any) => t.id === activeData.id);
        const newIndex = tasks.findIndex((t: any) => t.id === overData.id);

        if (oldIndex !== -1 && newIndex !== -1) {
          // Reordering within the SAME folder (ArrayMove naturally handles UP vs DOWN)
          const moved = arrayMove(tasks, oldIndex, newIndex);
          prevKey = newIndex > 0 ? moved[newIndex - 1]?.orderKey : undefined;
          nextKey = newIndex < moved.length - 1 ? moved[newIndex + 1]?.orderKey : undefined;
        } else if (newIndex !== -1) {
          // Moving from another folder, inserting AT specific position
          prevKey = newIndex > 0 ? tasks[newIndex - 1]?.orderKey : undefined;
          nextKey = newIndex < tasks.length ? tasks[newIndex]?.orderKey : undefined;
        } else {
          // Dropped onto a generic empty folder/space container, append to top
          prevKey = undefined;
          nextKey = tasks.length > 0 ? tasks[0]?.orderKey : undefined;
        }
      }

      newOrderKey = fractionalBetween(prevKey, nextKey);
      
      moveItem.mutate({
        itemId: activeData.id,
        itemType: EntityLayerConst.ProjectTask,
        targetParentId,
        previousItemOrderKey: prevKey,
        nextItemOrderKey: nextKey, // Provided for precise optimistic update sorting
        newOrderKey
      });
    }
  };

  return { sensors, handleDragStart, handleDragEnd, activeItem };
}

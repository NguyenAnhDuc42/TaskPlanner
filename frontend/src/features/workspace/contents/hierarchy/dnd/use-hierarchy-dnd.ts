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
import { fractionalAfter, fractionalBetween } from "../utils/fractional-index";

interface UseHierarchyDndProps {
  filteredHierarchy: WorkspaceHierarchy | undefined;
  moveItem: {
    mutate: (data: MoveItemRequest) => void;
  };
}

export function useHierarchyDnd({ filteredHierarchy, moveItem }: UseHierarchyDndProps) {
  const [activeItem, setActiveItem] = useState<{ 
    id: string, 
    type: EntityLayerType, 
    data: SpaceHierarchy | FolderHierarchy | TaskHierarchy 
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

    // Movement context
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

      // Vertical move within same or other space
      if (overData?.type === EntityLayerConst.ProjectSpace) {
        targetSpaceId = overData.id;
      } else if (overData?.type === EntityLayerConst.ProjectFolder) {
        targetSpaceId = overData.parentId;
      } else if (overData?.type === EntityLayerConst.ProjectTask) {
        // If dropped over a task, find the task's parent space
        const targetParent = overData.parentType === EntityLayerConst.ProjectSpace 
          ? overData.parentId 
          : filteredHierarchy?.spaces.find(s => s.folders.some(f => f.id === overData.parentId))?.id;
        targetSpaceId = targetParent;
      }

      if (!targetSpaceId) return;
      targetParentId = targetSpaceId;

      const targetSpace = filteredHierarchy?.spaces.find(s => s.id === targetSpaceId);
      if (!targetSpace) return;

      const folders = targetSpace.folders;
      const oldIndex = folders.findIndex(f => f.id === activeData.id);
      const newIndex = folders.findIndex(f => f.id === overData.id);

      // Reordering within same space
      if (oldIndex !== -1) {
        const moved = arrayMove(folders, oldIndex, newIndex === -1 ? 0 : newIndex);
        const finalIdx = newIndex === -1 ? 0 : newIndex;
        prevKey = finalIdx > 0 ? moved[finalIdx - 1]?.orderKey : undefined;
        nextKey = finalIdx < moved.length - 1 ? moved[finalIdx + 1]?.orderKey : undefined;
      } else {
        // Move to new space (append or insert)
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
        prevKey = overData.orderKey; // Use current item as reference
      } else if (overData?.type === EntityLayerConst.ProjectFolder || overData?.type === EntityLayerConst.ProjectSpace) {
        targetId = overData.id;
        prevKey = undefined; // Move to beginning
      }

      if (!targetId) return;
      targetParentId = targetId;
      newOrderKey = prevKey ? fractionalAfter(prevKey) : fractionalAfter(null);
      
      moveItem.mutate({
        itemId: activeData.id,
        itemType: EntityLayerConst.ProjectTask,
        targetParentId,
        previousItemOrderKey: prevKey,
        newOrderKey
      });
    }
  };

  return { sensors, handleDragStart, handleDragEnd, activeItem };
}

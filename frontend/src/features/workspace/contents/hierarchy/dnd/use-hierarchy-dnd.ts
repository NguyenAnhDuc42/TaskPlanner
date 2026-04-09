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
    setActiveItem({
      id: event.active.id as string,
      type: event.active.data.current?.type as EntityLayerType,
      data: event.active.data.current
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
      let targetSpace: SpaceHierarchy | undefined;

      // Vertical move within same or other space
      if (overData?.type === EntityLayerConst.ProjectSpace) {
        targetSpace = filteredHierarchy?.spaces.find(s => s.id === overData.id);
      } else if (overData?.type === EntityLayerConst.ProjectFolder) {
        targetSpace = filteredHierarchy?.spaces.find(s => s.folders.some(f => f.id === overData.id));
      }

      if (!targetSpace) return;
      targetParentId = targetSpace.id;

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
    // 3. REORDERING TASKS
    else if (itemType === EntityLayerConst.ProjectTask) {
      if (overData?.type !== EntityLayerConst.ProjectTask) return;
      
      // For now tasks only reorder within their current parent
      prevKey = overData.orderKey;
      newOrderKey = fractionalAfter(prevKey); // Simple strategy for now
      
      moveItem.mutate({
        itemId: activeData.id,
        itemType: EntityLayerConst.ProjectTask,
        targetParentId: activeData?.parentId,
        previousItemOrderKey: prevKey,
        newOrderKey
      });
    }
  };

  return { sensors, handleDragStart, handleDragEnd, activeItem };
}

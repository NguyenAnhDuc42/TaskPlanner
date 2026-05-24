import { 
  useSensor, 
  useSensors, 
  PointerSensor, 
  KeyboardSensor,
  type DragEndEvent,
  type DragStartEvent
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { useState } from "react";
import { useDebounceCallback } from "@/hooks/use-debounce";
import { handleSpaceMove } from "./handlers/handle-space-move";
import { handleFolderMove } from "./handlers/handle-folder-move";
import { handleTaskMove } from "./handlers/handle-task-move";
import type { DragItemData } from "./drag-item-type";
import type { MoveItemRequest } from "../hierarchy-api";

interface UseHierarchyDndProps {
  workspaceId: string;
  moveItem: {
    mutate: (data: MoveItemRequest) => void;
  };
}

export function useHierarchyDnd({ workspaceId, moveItem }: UseHierarchyDndProps) {
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);

  // Debounce the actual API call
  const debouncedMoveItem = useDebounceCallback(moveItem.mutate, 1000);

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

    setActiveItem(data as DragItemData);
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveItem(null);
    
    if (!over || active.id === over.id) return;

    const activeData = active.data.current as DragItemData | undefined;
    const overData = over.data.current as DragItemData | undefined;

    if (!activeData || !overData) return;

    if (activeData.type === EntityLayerConst.ProjectSpace) {
      handleSpaceMove(workspaceId, activeData, overData, debouncedMoveItem);
    } 
    else if (activeData.type === EntityLayerConst.ProjectFolder) {
      handleFolderMove(activeData, overData, debouncedMoveItem);
    }
    else if (activeData.type === EntityLayerConst.ProjectTask) {
      handleTaskMove(activeData, overData, debouncedMoveItem);
    }
  };

  return {
    sensors,
    handleDragStart,
    handleDragEnd,
    activeItem
  };
}

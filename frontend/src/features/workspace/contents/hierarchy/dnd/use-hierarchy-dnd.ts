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
import { useState, useRef, useEffect } from "react";
import { handleSpaceMove } from "./handlers/handle-space-move";
import { handleFolderMove } from "./handlers/handle-folder-move";
import { handleTaskMove } from "./handlers/handle-task-move";
import type { DragItemData } from "./drag-item-type";

interface UseHierarchyDndProps {
  workspaceId: string;
  batchMoveItems: (args: { workspaceId: string; moves: { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string }[] }) => Promise<unknown>;
}

export function useHierarchyDnd({ workspaceId, batchMoveItems }: UseHierarchyDndProps) {
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);

  // 1. Buffered moves Map (itemId -> move parameters) to avoid redundant requests
  const pendingMovesRef = useRef<Map<string, { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string }>>(new Map());
  const debounceTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Clean timeouts on unmount to prevent leaks
  useEffect(() => {
    return () => {
      if (debounceTimeoutRef.current) clearTimeout(debounceTimeoutRef.current);
    };
  }, []);

  // 2. Debounce trigger: consolidates drag actions in a 500ms window
  const triggerDebouncedBatchMove = (move: { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string }) => {
    pendingMovesRef.current.set(move.itemId, move);

    if (debounceTimeoutRef.current) clearTimeout(debounceTimeoutRef.current);

    debounceTimeoutRef.current = setTimeout(async () => {
      const movesToSend = Array.from(pendingMovesRef.current.values());
      pendingMovesRef.current.clear();

      if (movesToSend.length > 0) {
        try {
          await batchMoveItems({ workspaceId, moves: movesToSend });
        } catch (err) {
          console.error("[DND] Batch move network request failed:", err);
        }
      }
    }, 5000);
  };

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

    // Direct, type-safe movement routing using our debounced batch pipeline
    if (activeData.type === EntityLayerConst.ProjectSpace) {
      handleSpaceMove( activeData, overData, triggerDebouncedBatchMove);
    } 
    else if (activeData.type === EntityLayerConst.ProjectFolder) {
      handleFolderMove( activeData, overData, triggerDebouncedBatchMove);
    }
    else if (activeData.type === EntityLayerConst.ProjectTask) {
      handleTaskMove(activeData, overData, triggerDebouncedBatchMove);
    }
  };

  return {
    sensors,
    handleDragStart,
    handleDragEnd,
    activeItem
  };
}

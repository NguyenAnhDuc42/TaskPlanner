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
import type { BatchMoveCommand } from "../hierarchy-api";

// Discriminated union — each move type carries only what it needs
type PendingMove =
  | { kind: "space";  itemId: string; newOrderKey: string }
  | { kind: "folder"; itemId: string; targetParentId: string | null; newOrderKey: string }
  | { kind: "task";   itemId: string; targetSpaceId: string; targetFolderId: string | null; newOrderKey: string };

interface UseHierarchyDndProps {
  workspaceId: string;
  batchMoveItems: (args: { workspaceId: string; command: BatchMoveCommand }) => Promise<unknown>;
}

export function useHierarchyDnd({ workspaceId, batchMoveItems }: UseHierarchyDndProps) {
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);

  // Buffered moves Map (itemId → latest move for that item) to avoid redundant requests
  const pendingMovesRef = useRef<Map<string, PendingMove>>(new Map());
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

  // Debounce trigger: consolidates drag actions in a 500ms window,
  // then flushes into a typed BatchMoveCommand
  const triggerDebouncedBatchMove = (move: PendingMove) => {
    // Latest move for this item always wins
    pendingMovesRef.current.set(move.itemId, move);

    if (debounceTimeoutRef.current) clearTimeout(debounceTimeoutRef.current);

    debounceTimeoutRef.current = setTimeout(async () => {
      const movesToSend = Array.from(pendingMovesRef.current.values());
      pendingMovesRef.current.clear();

      if (movesToSend.length === 0) return;

      // Build typed command — group by kind
      const command: BatchMoveCommand = {
        spaces: movesToSend
          .filter((m): m is Extract<PendingMove, { kind: "space" }> => m.kind === "space")
          .map(m => ({ itemId: m.itemId, newOrderKey: m.newOrderKey })),
        folders: movesToSend
          .filter((m): m is Extract<PendingMove, { kind: "folder" }> => m.kind === "folder")
          .map(m => ({ itemId: m.itemId, targetParentId: m.targetParentId, newOrderKey: m.newOrderKey })),
        tasks: movesToSend
          .filter((m): m is Extract<PendingMove, { kind: "task" }> => m.kind === "task")
          .map(m => ({ itemId: m.itemId, targetSpaceId: m.targetSpaceId, targetFolderId: m.targetFolderId, newOrderKey: m.newOrderKey })),
      };

      try {
        await batchMoveItems({ workspaceId, command });
      } catch (err) {
        console.error("[DND] Batch move network request failed:", err);
      }
    }, 1000);
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
      handleSpaceMove(activeData, overData, triggerDebouncedBatchMove);
    } 
    else if (activeData.type === EntityLayerConst.ProjectFolder) {
      handleFolderMove(activeData, overData, triggerDebouncedBatchMove);
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

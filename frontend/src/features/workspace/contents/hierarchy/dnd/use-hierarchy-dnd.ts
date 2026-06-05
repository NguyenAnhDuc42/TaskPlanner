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
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import { store } from "@/store";
import { spaceSlice, folderSlice, taskSlice } from "@/store/entityStore";
import { toast } from "sonner";

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

  const originalSpacesRef = useRef<Map<string, SpaceRecord>>(new Map());
  const originalFoldersRef = useRef<Map<string, FolderRecord>>(new Map());
  const originalTasksRef = useRef<Map<string, TaskRecord>>(new Map());

  const recordOriginalSpace = (id: string) => {
    if (originalSpacesRef.current.has(id)) return;
    const entity = store.getState().spaces.entities[id];
    if (entity) {
      originalSpacesRef.current.set(id, { ...entity });
    }
  };

  const recordOriginalFolder = (id: string) => {
    if (originalFoldersRef.current.has(id)) return;
    const entity = store.getState().folders.entities[id];
    if (entity) {
      originalFoldersRef.current.set(id, { ...entity });
    }
  };

  const recordOriginalTask = (id: string) => {
    if (originalTasksRef.current.has(id)) return;
    const entity = store.getState().tasks.entities[id];
    if (entity) {
      originalTasksRef.current.set(id, { ...entity });
    }
  };

  const rollback = () => {
    if (originalSpacesRef.current.size > 0) {
      const spaces = Array.from(originalSpacesRef.current.values());
      store.dispatch(spaceSlice.actions.upsertMany(spaces));
      originalSpacesRef.current.clear();
    }
    if (originalFoldersRef.current.size > 0) {
      const folders = Array.from(originalFoldersRef.current.values());
      store.dispatch(folderSlice.actions.upsertMany(folders));
      originalFoldersRef.current.clear();
    }
    if (originalTasksRef.current.size > 0) {
      const tasks = Array.from(originalTasksRef.current.values());
      store.dispatch(taskSlice.actions.upsertMany(tasks));
      originalTasksRef.current.clear();
    }
  };

  const clearOriginals = () => {
    originalSpacesRef.current.clear();
    originalFoldersRef.current.clear();
    originalTasksRef.current.clear();
  };

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 3 },
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
        const result = (await batchMoveItems({ workspaceId, command })) as {
          error?: { data?: { Description?: string; detail?: string } };
        };
        if (result.error) {
          console.error("[DND] Batch move mutation failed, rolling back:", result.error);
          const errMsg = result.error.data?.Description || result.error.data?.detail || "Failed to move items";
          toast.error(errMsg);
          rollback();
        } else {
          clearOriginals();
        }
      } catch (err) {
        console.error("[DND] Batch move network request failed, rolling back:", err);
        toast.error("Network error occurred while moving items");
        rollback();
      }
    }, 4000);
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
      recordOriginalSpace(activeData.id);
      handleSpaceMove(activeData, overData, triggerDebouncedBatchMove);
    } 
    else if (activeData.type === EntityLayerConst.ProjectFolder) {
      recordOriginalFolder(activeData.id);
      recordOriginalSpace(activeData.spaceId);
      if (overData.type === EntityLayerConst.ProjectSpace) {
        recordOriginalSpace(overData.id);
      } else if (overData.type === EntityLayerConst.ProjectFolder) {
        recordOriginalSpace(overData.spaceId);
      }
      handleFolderMove(activeData, overData, triggerDebouncedBatchMove);
    }
    else if (activeData.type === EntityLayerConst.ProjectTask) {
      recordOriginalTask(activeData.id);
      if (activeData.parentType === EntityLayerConst.ProjectFolder) {
        recordOriginalFolder(activeData.parentId);
      } else {
        recordOriginalSpace(activeData.parentId);
      }
      let targetSpaceId: string | undefined;
      let targetFolderId: string | null = null;
      if (overData.type === EntityLayerConst.ProjectSpace) {
        targetSpaceId = overData.id;
      } else if (overData.type === EntityLayerConst.ProjectFolder) {
        targetSpaceId = overData.spaceId;
        targetFolderId = overData.id;
      } else if (overData.type === EntityLayerConst.ProjectTask) {
        targetSpaceId = overData.spaceId;
        targetFolderId = overData.parentType === EntityLayerConst.ProjectFolder ? overData.parentId : null;
      }
      if (targetFolderId) {
        recordOriginalFolder(targetFolderId);
      } else if (targetSpaceId) {
        recordOriginalSpace(targetSpaceId);
      }
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

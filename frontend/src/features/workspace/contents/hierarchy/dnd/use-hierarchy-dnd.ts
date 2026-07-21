import {
  useSensor,
  useSensors,
  PointerSensor,
  TouchSensor,
  KeyboardSensor,
  type DragEndEvent,
  type DragStartEvent,
  type DragMoveEvent,
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import { useState } from "react";
import { handleDocumentMove, type DropZone } from "./handlers/handle-document-move";
import type { DragItemData } from "./drag-item-type";
import { rescueSwallowedClick } from "@/lib/dnd-click-rescue";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { DocumentMutations } from "@/mutations/document.mutations";
import { useMemo } from "react";

// The top/bottom quarters of the target row reorder as a sibling before/after it; the middle
// half nests the dragged item as its child. Uses the dragged row's *initial* rect (its resting
// position before the drag started, which never changes mid-gesture) plus the drag's accumulated
// delta, rather than active.rect.current.translated — that rect gets re-measured as
// SortableContext's own live reorder preview shifts rows to make space, and (separately) the
// DragOverlay's scale-105 transform on the ghost row inflates its measured size, so it doesn't
// track the pointer as reliably as initial + delta does.
function computeDropZone(
  overRect: { top: number; height: number },
  activeInitialRect: { top: number; height: number } | null,
  deltaY: number,
): DropZone {
  if (!activeInitialRect) return "before";
  const activeCenterY = activeInitialRect.top + activeInitialRect.height / 2 + deltaY;
  const ratio = (activeCenterY - overRect.top) / overRect.height;
  if (ratio < 0.25) return "before";
  if (ratio > 0.75) return "after";
  return "nest";
}

export function useHierarchyDnd() {
  const { canCreateContent } = useWorkspaceRole();
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);
  const [dropTarget, setDropTarget] = useState<{ id: string; zone: DropZone } | null>(null);

  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const documentMutations = useMemo(() => new DocumentMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const pointerSensor = useSensor(PointerSensor, { activationConstraint: { distance: 8 } });
  const touchSensor = useSensor(TouchSensor, { activationConstraint: { delay: 150, tolerance: 5 } });
  const keyboardSensor = useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates });
  const sensors = useSensors(pointerSensor, touchSensor, keyboardSensor);

  const handleDragStart = (event: DragStartEvent) => {
    if (!canCreateContent) return;
    const data = event.active.data.current;
    if (!data) return;
    setActiveItem(data as DragItemData);
  };

  // onDragMove (not onDragOver) — onDragOver's dispatch is keyed on the "over" id changing, so it
  // never re-fires for continued pointer movement within the same target row; onDragMove fires on
  // every movement, which is what the before/nest/after zone needs to stay live as the pointer
  // moves around inside one row rather than only updating right as it first crosses into it.
  const handleDragMove = (event: DragMoveEvent) => {
    if (!canCreateContent) return;
    const { active, over } = event;
    if (!over || active.id === over.id) {
      setDropTarget(null);
      return;
    }
    const zone = computeDropZone(over.rect, active.rect.current.initial, event.delta.y);
    setDropTarget((prev) => (prev?.id === over.id && prev.zone === zone ? prev : { id: String(over.id), zone }));
  };

  const handleDragEnd = (event: DragEndEvent) => {
    if (!canCreateContent) { setActiveItem(null); setDropTarget(null); return; }
    const { active, over } = event;

    rescueSwallowedClick(event);

    if (over && active.id !== over.id && dropTarget) {
      const activeData = active.data.current as DragItemData | undefined;
      const overData = over.data.current as DragItemData | undefined;

      if (activeData && overData) {
        handleDocumentMove(rootStore, documentMutations, activeData, overData, dropTarget.zone);
        scheduleFlush();
      }
    }

    setActiveItem(null);
    setDropTarget(null);
  };

  return {
    sensors,
    handleDragStart,
    handleDragMove,
    handleDragEnd,
    activeItem,
    dropTarget,
  };
}

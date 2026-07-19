import {
  useSensor,
  useSensors,
  PointerSensor,
  TouchSensor,
  KeyboardSensor,
  type DragEndEvent,
  type DragStartEvent
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import { useState } from "react";
import { handleDocumentMove } from "./handlers/handle-document-move";
import type { DragItemData } from "./drag-item-type";
import { rescueSwallowedClick } from "@/lib/dnd-click-rescue";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { DocumentMutations } from "@/mutations/document.mutations";
import { useMemo } from "react";

export function useHierarchyDnd() {
  const { canCreateContent } = useWorkspaceRole();
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);

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

  const handleDragEnd = (event: DragEndEvent) => {
    if (!canCreateContent) { setActiveItem(null); return; }
    const { active, over } = event;

    rescueSwallowedClick(event);

    if (over && active.id !== over.id) {
      const activeData = active.data.current as DragItemData | undefined;
      const overData = over.data.current as DragItemData | undefined;

      if (activeData && overData) {
        handleDocumentMove(rootStore, documentMutations, activeData, overData);
        scheduleFlush();
      }
    }

    setActiveItem(null);
  };

  return {
    sensors,
    handleDragStart,
    handleDragEnd,
    activeItem
  };
}

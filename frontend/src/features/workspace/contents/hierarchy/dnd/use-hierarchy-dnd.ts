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
import { handleSpaceMove } from "./handlers/handle-space-move";
import { handleFolderMove } from "./handlers/handle-folder-move";
import { handleTaskMove } from "./handlers/handle-task-move";
import type { DragItemData } from "./drag-item-type";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { SpaceMutations } from "@/mutations/space.mutations";
import { FolderMutations } from "@/mutations/folder.mutations";
import { TaskMutations } from "@/mutations/task.mutations";
import { useMemo } from "react";

export function useHierarchyDnd() {
  const { canCreateContent } = useWorkspaceRole();
  const [activeItem, setActiveItem] = useState<DragItemData | null>(null);

  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const folderMutations = useMemo(() => new FolderMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const pointerSensor = useSensor(PointerSensor, { activationConstraint: { distance: 8 } });
  const keyboardSensor = useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates });
  const sensors = useSensors(pointerSensor, keyboardSensor);

  const handleDragStart = (event: DragStartEvent) => {
    if (!canCreateContent) return;
    const data = event.active.data.current;
    if (!data) return;
    setActiveItem(data as DragItemData);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    if (!canCreateContent) { setActiveItem(null); return; }
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const activeData = active.data.current as DragItemData | undefined;
      const overData = over.data.current as DragItemData | undefined;

      if (activeData && overData) {
        if (activeData.type === EntityLayerConst.ProjectSpace) {
          handleSpaceMove(rootStore, spaceMutations, activeData, overData);
        } else if (activeData.type === EntityLayerConst.ProjectFolder) {
          handleFolderMove(rootStore, folderMutations, taskMutations, activeData, overData);
        } else if (activeData.type === EntityLayerConst.ProjectTask) {
          handleTaskMove(rootStore, taskMutations, activeData, overData);
        }
        // Each handler queues a local update (store + IndexedDB + enqueue) via *Mutations
        // .updateLocal() — one shared debounced flush sends everything in one network call,
        // same "N local writes, one flush" pattern used everywhere else in the sync engine.
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

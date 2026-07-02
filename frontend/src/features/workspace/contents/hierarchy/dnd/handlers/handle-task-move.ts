import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { DragItemData, DragTaskData } from "../drag-item-type";
import type { RootStore } from "@/stores/root.store";
import type { TaskMutations } from "@/mutations/task.mutations";
import type { TaskRecord } from "@/types/projects";

export function handleTaskMove(
  rootStore: RootStore,
  taskMutations: TaskMutations,
  activeData: DragTaskData,
  overData: DragItemData,
): void {
  // Derive explicit targetSpaceId + optional targetFolderId.
  // Space is primary container; Folder is optional organizational layer.
  let targetSpaceId: string | undefined;
  let targetFolderId: string | null = null;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId;
    targetFolderId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectTask) {
    targetSpaceId = overData.spaceId;
    targetFolderId = overData.parentType === EntityLayerConst.ProjectFolder ? overData.parentId : null;
  }

  if (!targetSpaceId) return;

  const sourceContainerId = activeData.parentId;
  const isSourceFolder = activeData.parentType === EntityLayerConst.ProjectFolder;
  const targetContainerId = targetFolderId ?? targetSpaceId;
  const isTargetFolder = !!targetFolderId;
  const isSameContainer = sourceContainerId === targetContainerId;

  const filterForContainer = (containerId: string, isFolder: boolean) =>
    rootStore.taskStore.all
      .filter((t) => !t.parentTaskId && (isFolder ? t.folderId === containerId : (t.spaceId === containerId && !t.folderId)))
      .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const sourceTasksList = filterForContainer(sourceContainerId, isSourceFolder);
  const sourceTasks = sourceTasksList.map(t => t.id);

  const targetTasksList = isSameContainer ? sourceTasksList : filterForContainer(targetContainerId, isTargetFolder);
  const targetTasks = targetTasksList.map(t => t.id);

  // Guard: dropping onto itself or its current parent header
  if (activeData.id === overData.id || overData.id === activeData.parentId) {
    return;
  }

  const oldIndex = sourceTasks.indexOf(activeData.id);
  let newIndex = targetTasks.indexOf(overData.id);
  // If over a parent row (folder/space header), append to end
  if (newIndex === -1) newIndex = targetTasks.length;

  if (isSameContainer && oldIndex === newIndex) {
    return;
  }

  let newOrderKey: string;
  const patch: { orderKey: string; spaceId?: string; folderId?: string | null } = { orderKey: "" };

  if (isSameContainer) {
    if (oldIndex === -1) return;

    const moved = arrayMove(sourceTasksList, oldIndex, newIndex);
    const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
    const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
    newOrderKey = fractionalBetween(prevKey, nextKey);
    patch.orderKey = newOrderKey;
  } else {
    // Cross-container move: insert at newIndex in target list
    const newTargetTasks: (TaskRecord | undefined)[] = [...targetTasksList];
    newTargetTasks.splice(newIndex, 0, sourceTasksList.find(t => t.id === activeData.id));

    const prevKey = safeKey(newIndex > 0 ? newTargetTasks[newIndex - 1]?.orderKey : undefined);
    const nextKey = safeKey(newIndex < newTargetTasks.length - 1 ? newTargetTasks[newIndex + 1]?.orderKey : undefined);
    newOrderKey = fractionalBetween(prevKey, nextKey);

    patch.orderKey = newOrderKey;
    patch.spaceId = targetSpaceId;
    patch.folderId = targetFolderId;
  }

  taskMutations.updateLocal(activeData.id, patch).catch((err) => console.error("Failed to move task", err));
}

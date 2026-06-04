import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { generateKeyBetween } from "fractional-indexing";
import { safeKey } from "../../utils/fractional-index";
import type { DragItemData, DragTaskData } from "../drag-item-type";
import { store } from "@/store";
import { folderSlice, spaceSlice, taskSlice } from "@/store/entityStore";

export function handleTaskMove(
  activeData: DragTaskData,
  overData: DragItemData,
  triggerBatchMove: (move: { kind: "task"; itemId: string; targetSpaceId: string; targetFolderId: string | null; newOrderKey: string }) => void
) {
  // Derive explicit targetSpaceId + optional targetFolderId
  // Space is primary container; Folder is optional organizational layer
  let targetSpaceId: string | undefined;
  let targetFolderId: string | null = null;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
    targetFolderId = null;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId; // folder's parent space
    targetFolderId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectTask) {
    targetSpaceId = overData.spaceId;
    targetFolderId = overData.parentType === EntityLayerConst.ProjectFolder ? overData.parentId : null;
  }

  if (!targetSpaceId) return;

  const state = store.getState();
  const sourceContainerId = activeData.parentId;
  const isSourceFolder = activeData.parentType === EntityLayerConst.ProjectFolder;
  const targetContainerId = targetFolderId ?? targetSpaceId;
  const isTargetFolder = !!targetFolderId;
  const isSameContainer = sourceContainerId === targetContainerId;

  // Retrieve and sort tasks for source container
  const sourceTasks = Object.values(state.tasks.entities)
    .filter((t): t is typeof t & { id: string } => {
      if (!t) return false;
      return isSourceFolder
        ? (t.folderId === sourceContainerId)
        : (t.spaceId === sourceContainerId && !t.folderId);
    })
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
    .map(t => t.id);

  // Retrieve and sort tasks for target container
  const targetTasks = isSameContainer
    ? sourceTasks
    : Object.values(state.tasks.entities)
        .filter((t): t is typeof t & { id: string } => {
          if (!t) return false;
          return isTargetFolder
            ? (t.folderId === targetContainerId)
            : (t.spaceId === targetContainerId && !t.folderId);
        })
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
        .map(t => t.id);

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

  if (isSameContainer) {
    if (oldIndex === -1) return;

    // Retrieve sorted task records to read neighbor order keys
    const sortedTasks = sourceTasks.map(id => state.tasks.entities[id]).filter((t): t is typeof t & { orderKey?: string } => !!t);
    const moved = arrayMove(sortedTasks, oldIndex, newIndex);

    const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
    const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
    newOrderKey = generateKeyBetween(prevKey, nextKey);

    // Optimistic: update only the dragged task
    store.dispatch(taskSlice.actions.upsert({ id: activeData.id, orderKey: newOrderKey }));
  } else {
    // Cross-container move: insert at newIndex in target list
    const newTargetTasks = [...targetTasks];
    newTargetTasks.splice(newIndex, 0, activeData.id);

    const prevKey = safeKey(newIndex > 0 ? state.tasks.entities[newTargetTasks[newIndex - 1]]?.orderKey : undefined);
    const nextKey = safeKey(newIndex < newTargetTasks.length - 1 ? state.tasks.entities[newTargetTasks[newIndex + 1]]?.orderKey : undefined);
    newOrderKey = generateKeyBetween(prevKey, nextKey);

    // Optimistic: update only the dragged task
    store.dispatch(taskSlice.actions.upsert({
      id: activeData.id,
      folderId: targetFolderId,
      spaceId: targetSpaceId,
      orderKey: newOrderKey
    }));
  }

  // Turn ON hasTasks for new container
  if (isTargetFolder) {
    store.dispatch(folderSlice.actions.upsert({ id: targetContainerId, hasTasks: true }));
  } else {
    store.dispatch(spaceSlice.actions.upsert({ id: targetContainerId, hasTasks: true }));
  }

  // Turn OFF hasTasks for old container if no tasks remain
  if (!isSameContainer) {
    const remainingTasks = sourceTasks.filter(id => id !== activeData.id);
    if (remainingTasks.length === 0) {
      if (isSourceFolder) {
        const sourceFolder = state.folders.entities[sourceContainerId];
        if (sourceFolder) store.dispatch(folderSlice.actions.upsert({ ...sourceFolder, hasTasks: false }));
      } else {
        const sourceSpace = state.spaces.entities[sourceContainerId];
        if (sourceSpace) store.dispatch(spaceSlice.actions.upsert({ ...sourceSpace, hasTasks: false }));
      }
    }
  }

  // Trigger batch queue
  triggerBatchMove({
    kind: "task",
    itemId: activeData.id,
    targetSpaceId,
    targetFolderId,
    newOrderKey
  });
}

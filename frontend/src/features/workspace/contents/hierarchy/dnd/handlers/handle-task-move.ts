import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { generateKeyBetween, generateNKeysBetween } from "fractional-indexing";
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
        ? (t.projectFolderId === sourceContainerId)
        : (t.projectSpaceId === sourceContainerId && !t.projectFolderId);
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
            ? (t.projectFolderId === targetContainerId)
            : (t.projectSpaceId === targetContainerId && !t.projectFolderId);
        })
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
        .map(t => t.id);

  const oldIndex = sourceTasks.indexOf(activeData.id);
  let newIndex = targetTasks.indexOf(overData.id);
  // If over a parent row (folder/space header), append to end
  if (newIndex === -1) newIndex = targetTasks.length;

  let newOrderKey: string;

  if (isSameContainer) {
    // Same-container reorder: regenerate ALL keys so numeric/rocicorp mix stays sorted
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceTasks, oldIndex, newIndex);
    const freshKeys = generateNKeysBetween(null, null, moved.length);
    newOrderKey = freshKeys[newIndex];
    // Optimistic: update ALL tasks in this container
    moved.forEach((id, i) => {
      store.dispatch(taskSlice.actions.upsert({ id, orderKey: freshKeys[i] }));
    });
  } else {
    // Cross-container move: insert at newIndex in target list
    const newTargetTasks = [...targetTasks];
    newTargetTasks.splice(newIndex, 0, activeData.id);

    const prevKey = safeKey(newIndex > 0 ? state.tasks.entities[newTargetTasks[newIndex - 1]]?.orderKey : undefined);
    const nextKey = safeKey(newIndex < newTargetTasks.length - 1 ? state.tasks.entities[newTargetTasks[newIndex + 1]]?.orderKey : undefined);

    if (prevKey !== null || nextKey !== null) {
      // At least one neighbor has a valid rocicorp key — insert between them
      newOrderKey = generateKeyBetween(prevKey, nextKey);
    } else {
      // All existing target keys are legacy numeric — generate fresh sequence and pick position
      const freshKeys = generateNKeysBetween(null, null, newTargetTasks.length);
      newOrderKey = freshKeys[newIndex];
    }

    // Optimistic: update the dragged task — explicit space + folder, no lookup needed
    store.dispatch(taskSlice.actions.upsert({
      id: activeData.id,
      projectFolderId: targetFolderId,
      projectSpaceId: targetSpaceId,
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

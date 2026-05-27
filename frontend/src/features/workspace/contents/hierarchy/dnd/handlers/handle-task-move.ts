import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst, EntityLayerType } from "@/types/entity-layer-type";
import { generateKeyBetween, generateNKeysBetween } from "fractional-indexing";
import { safeKey } from "../../utils/fractional-index";
import type { DragItemData, DragTaskData } from "../drag-item-type";
import { store } from "@/store";
import { folderSlice, spaceSlice, taskSlice } from "@/store/entityStore";

export function handleTaskMove(
  activeData: DragTaskData,
  overData: DragItemData,
  triggerBatchMove: (move: { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string; }) => void
) {
  let targetParentId: string | undefined;
  let targetParentType: EntityLayerType | undefined;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetParentId = overData.id;
    targetParentType = EntityLayerConst.ProjectSpace;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetParentId = overData.id;
    targetParentType = EntityLayerConst.ProjectFolder;
  } else if (overData?.type === EntityLayerConst.ProjectTask) {
    targetParentId = overData.parentId;
    targetParentType = overData.parentType;
  }

  if (!targetParentId || !targetParentType) return;

  const state = store.getState();
  const sourceParentId = activeData.parentId;
  const isSourceSpace = activeData.parentType === EntityLayerConst.ProjectSpace;
  const isTargetSpace = targetParentType === EntityLayerConst.ProjectSpace;
  const isTargetFolder = targetParentType === EntityLayerConst.ProjectFolder;
  const isSameParent = sourceParentId === targetParentId && isSourceSpace === isTargetSpace;

  // Retrieve and sort tasks for source parent
  const sourceTasks = Object.values(state.tasks.entities)
    .filter((t): t is typeof t & { id: string } => {
      if (!t) return false;
      return isSourceSpace
        ? (t.projectSpaceId === sourceParentId && !t.projectFolderId)
        : (t.projectFolderId === sourceParentId);
    })
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
    .map(t => t.id);

  // Retrieve and sort tasks for target parent
  const targetTasks = isSameParent
    ? sourceTasks
    : Object.values(state.tasks.entities)
        .filter((t): t is typeof t & { id: string } => {
          if (!t) return false;
          return isTargetSpace
            ? (t.projectSpaceId === targetParentId && !t.projectFolderId)
            : (t.projectFolderId === targetParentId);
        })
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
        .map(t => t.id);

  const oldIndex = sourceTasks.indexOf(activeData.id);
  let newIndex = targetTasks.indexOf(overData.id);
  // If over a parent row (folder/space header), append to end
  if (newIndex === -1) newIndex = targetTasks.length;

  let newOrderKey: string;

  if (isSameParent) {
    // Same-parent reorder: regenerate ALL keys so numeric/rocicorp mix stays sorted
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceTasks, oldIndex, newIndex);
    const freshKeys = generateNKeysBetween(null, null, moved.length);
    newOrderKey = freshKeys[newIndex];
    // Optimistic: update ALL tasks in this parent
    moved.forEach((id, i) => {
      store.dispatch(taskSlice.actions.upsert({ id, orderKey: freshKeys[i] }));
    });
  } else {
    // Cross-parent move: insert at newIndex in target list
    const newTargetTasks = [...targetTasks, ""].slice(0, targetTasks.length); // copy
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

    // Optimistic: update the dragged task's parent and order
    store.dispatch(taskSlice.actions.upsert({
      id: activeData.id,
      projectFolderId: isTargetFolder ? targetParentId : null,
      projectSpaceId: isTargetFolder ? state.folders.entities[targetParentId]?.parentId : targetParentId,
      parentType: targetParentType,
      orderKey: newOrderKey
    }));
  }

  // Turn ON hasTasks for new parent
  if (isTargetFolder) {
    store.dispatch(folderSlice.actions.upsert({ id: targetParentId, hasTasks: true }));
  } else {
    store.dispatch(spaceSlice.actions.upsert({ id: targetParentId, hasTasks: true }));
  }

  // Turn OFF hasTasks for old parent if no tasks remain
  if (!isSameParent) {
    const remainingTasks = sourceTasks.filter(id => id !== activeData.id);
    if (remainingTasks.length === 0) {
      if (isSourceSpace) {
        const sourceSpace = state.spaces.entities[sourceParentId];
        if (sourceSpace) store.dispatch(spaceSlice.actions.upsert({ ...sourceSpace, hasTasks: false }));
      } else {
        const sourceFolder = state.folders.entities[sourceParentId];
        if (sourceFolder) store.dispatch(folderSlice.actions.upsert({ ...sourceFolder, hasTasks: false }));
      }
    }
  }

  // Trigger batch queue
  triggerBatchMove({
    itemId: activeData.id,
    itemType: EntityLayerConst.ProjectTask,
    targetParentId: targetParentId,
    newOrderKey
  });
}

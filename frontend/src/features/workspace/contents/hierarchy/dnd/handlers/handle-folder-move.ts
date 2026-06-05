import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { DragItemData, DragFolderData } from "../drag-item-type";
import { store } from "@/store";
import { folderSlice, spaceSlice } from "@/store/entityStore";

export function handleFolderMove(
  activeData: DragFolderData,
  overData: DragItemData,
  triggerBatchMove: (move: { kind: "folder"; itemId: string; targetParentId: string | null; newOrderKey: string }) => void
) {
  let targetSpaceId: string | undefined;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId;
  }

  if (!targetSpaceId) return;

  const state = store.getState();
  const sourceSpaceId = activeData.spaceId;

  // Retrieve and sort current sibling folders under source space
  const sourceFoldersList = Object.values(state.folders.entities)
    .filter((f): f is typeof f & { id: string } => !!f && f.spaceId === sourceSpaceId)
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));

  const sourceFolders = sourceFoldersList.map(f => f.id);

  // Retrieve sibling folders under target space
  const targetFoldersList = sourceSpaceId === targetSpaceId
    ? sourceFoldersList
    : Object.values(state.folders.entities)
        .filter((f): f is typeof f & { id: string } => !!f && f.spaceId === targetSpaceId)
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));

  const targetFolders = targetFoldersList.map(f => f.id);

  // Guard: dropping onto itself or its current parent space header
  if (activeData.id === overData.id || overData.id === activeData.spaceId) {
    return;
  }

  const oldIndex = sourceFolders.indexOf(activeData.id);
  let newIndex = targetFolders.indexOf(overData.id);

  // Bug 2 Fix: If dropped on container space header, append to the end
  if (newIndex === -1) newIndex = targetFolders.length;

  if (sourceSpaceId === targetSpaceId && oldIndex === newIndex) {
    return;
  }

  let newOrderKey: string;

  if (sourceSpaceId === targetSpaceId) {
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceFoldersList, oldIndex, newIndex);
    const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
    const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
    newOrderKey = fractionalBetween(prevKey, nextKey);

    // Optimistic: update only the dragged folder
    store.dispatch(folderSlice.actions.upsert({ id: activeData.id, orderKey: newOrderKey }));
  } else {
    // Cross-space move: insert at newIndex in target list
    const prevKey = safeKey(targetFoldersList[newIndex - 1]?.orderKey);
    const nextKey = safeKey(targetFoldersList[newIndex]?.orderKey);
    newOrderKey = fractionalBetween(prevKey, nextKey);

    // Optimistic: update only the dragged folder
    store.dispatch(folderSlice.actions.upsert({ id: activeData.id, spaceId: targetSpaceId, orderKey: newOrderKey }));
  }

  // Turn ON hasFolders for new target Space
  store.dispatch(spaceSlice.actions.upsert({ id: targetSpaceId, hasFolders: true }));

  // Turn OFF hasFolders for source Space if this was the last folder under it
  const remainingFolders = sourceFolders.filter(id => id !== activeData.id);
  if (remainingFolders.length === 0) {
    const sourceSpace = state.spaces.entities[sourceSpaceId];
    if (sourceSpace) {
      store.dispatch(spaceSlice.actions.upsert({ ...sourceSpace, hasFolders: false }));
    }
  }

  // Trigger batch queue
  triggerBatchMove({ kind: "folder", itemId: activeData.id, targetParentId: targetSpaceId, newOrderKey });
}

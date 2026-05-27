import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { generateNKeysBetween } from "fractional-indexing";
import type { DragItemData, DragFolderData } from "../drag-item-type";
import { store } from "@/store";
import { folderSlice, spaceSlice } from "@/store/entityStore";

export function handleFolderMove( activeData: DragFolderData, overData: DragItemData, triggerBatchMove: (move: { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string; }) => void) {
  let targetSpaceId: string | undefined;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId;
  }

  if (!targetSpaceId) return;

  const state = store.getState();
  const sourceSpaceId = activeData.spaceId || activeData.parentId;

  // Retrieve and sort current sibling folders under source space
  const sourceFolders = Object.values(state.folders.entities)
    .filter((f): f is typeof f & { id: string } => !!f && f.parentId === sourceSpaceId)
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
    .map(f => f.id);

  // Retrieve sibling folders under target space
  const targetFolders = sourceSpaceId === targetSpaceId
    ? sourceFolders
    : Object.values(state.folders.entities)
        .filter((f): f is typeof f & { id: string } => !!f && f.parentId === targetSpaceId)
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
        .map(f => f.id);

  const oldIndex = sourceFolders.indexOf(activeData.id);
  let newIndex = targetFolders.indexOf(overData.id);
  if (newIndex === -1) newIndex = 0;

  let newOrderKey: string;

  if (sourceSpaceId === targetSpaceId) {
    // Same-space reorder: regenerate ALL folder keys so numeric→rocicorp mix stays sorted
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceFolders, oldIndex, newIndex);
    const freshKeys = generateNKeysBetween(null, null, moved.length);
    newOrderKey = freshKeys[newIndex];

    // Optimistic: update ALL folders in this space
    moved.forEach((id, i) => {
      store.dispatch(folderSlice.actions.upsert({ id, orderKey: freshKeys[i] }));
    });
  } else {
    // Cross-space move: insert at newIndex in target list
    const newTargetFolders = [...targetFolders];
    newTargetFolders.splice(newIndex, 0, activeData.id);
    const freshKeys = generateNKeysBetween(null, null, newTargetFolders.length);
    newOrderKey = freshKeys[newIndex];

    // Optimistic: update dragged folder + all existing target folders
    store.dispatch(folderSlice.actions.upsert({ id: activeData.id, parentId: targetSpaceId, orderKey: newOrderKey }));
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
  triggerBatchMove({
    itemId: activeData.id,
    itemType: EntityLayerConst.ProjectFolder,
    targetParentId: targetSpaceId,
    newOrderKey
  });
}

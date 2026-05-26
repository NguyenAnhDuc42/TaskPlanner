import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { fractionalBetween } from "../../utils/fractional-index";
import type { MoveItemRequest } from "../../hierarchy-api";
import type { DragItemData, DragFolderData } from "../drag-item-type";
import { store } from "@/store";
import { folderSlice } from "@/store/entityStore";

export function handleFolderMove(
  workspaceId: string,
  activeData: DragFolderData,
  overData: DragItemData,
  moveItemMutation: (args: { workspaceId: string; body: MoveItemRequest }) => Promise<unknown>
) {
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

  let prevKey: string | undefined;
  let nextKey: string | undefined;
  let newOrderKey: string | undefined;

  if (sourceSpaceId === targetSpaceId) {
    if (oldIndex === -1 || newIndex === -1) return;
    const moved = arrayMove(sourceFolders, oldIndex, newIndex);
    
    prevKey = newIndex > 0 ? state.folders.entities[moved[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < moved.length - 1 ? state.folders.entities[moved[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);
  } else {
    const newTargetFolders = [...targetFolders];
    newTargetFolders.splice(newIndex, 0, activeData.id);

    prevKey = newIndex > 0 ? state.folders.entities[newTargetFolders[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < newTargetFolders.length - 1 ? state.folders.entities[newTargetFolders[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);
  }

  // 1. Optimistic Update directly to Redux database
  store.dispatch(folderSlice.actions.upsert({ id: activeData.id, parentId: targetSpaceId, orderKey: newOrderKey }));

  // 2. Trigger RTK Query mutation (failsafe rollback fully automated in hierarchyApi)
  moveItemMutation({
    workspaceId,
    body: {
      itemId: activeData.id,
      itemType: EntityLayerConst.ProjectFolder,
      targetParentId: targetSpaceId,
      targetParentType: EntityLayerConst.ProjectSpace,
      nextItemOrderKey: nextKey,
      newOrderKey,
      sourceParentId: sourceSpaceId,
      sourceParentType: EntityLayerConst.ProjectSpace
    }
  });
}

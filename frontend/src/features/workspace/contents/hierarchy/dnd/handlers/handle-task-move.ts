import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst, EntityLayerType } from "@/types/entity-layer-type";
import { fractionalBetween } from "../../utils/fractional-index";
import type { MoveItemRequest } from "../../hierarchy-api";
import type { DragItemData, DragTaskData } from "../drag-item-type";
import { store } from "@/store";
import { taskSlice } from "@/store/entityStore";

export function handleTaskMove(
  workspaceId: string,
  activeData: DragTaskData,
  overData: DragItemData,
  moveItemMutation: (args: { workspaceId: string; body: MoveItemRequest }) => Promise<unknown>
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
  
  // Retrieve and sort tasks
  const sourceTasks = Object.values(state.tasks.entities)
    .filter((t): t is typeof t & { id: string } => !!t && t.projectFolderId === sourceParentId)
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
    .map(t => t.id);

  const targetTasks = sourceParentId === targetParentId 
    ? sourceTasks 
    : Object.values(state.tasks.entities)
        .filter((t): t is typeof t & { id: string } => !!t && t.projectFolderId === targetParentId)
        .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""))
        .map(t => t.id);

  const oldIndex = sourceTasks.indexOf(activeData.id);
  let newIndex = targetTasks.indexOf(overData.id);
  if (newIndex === -1) newIndex = 0;

  let prevKey: string | undefined;
  let nextKey: string | undefined;
  let newOrderKey: string | undefined;

  if (sourceParentId === targetParentId) {
    if (oldIndex === -1 || newIndex === -1) return;
    const moved = arrayMove(sourceTasks, oldIndex, newIndex);
    
    prevKey = newIndex > 0 ? state.tasks.entities[moved[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < moved.length - 1 ? state.tasks.entities[moved[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);
  } else {
    const newTargetTasks = [...targetTasks];
    newTargetTasks.splice(newIndex, 0, activeData.id);

    prevKey = newIndex > 0 ? state.tasks.entities[newTargetTasks[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < newTargetTasks.length - 1 ? state.tasks.entities[newTargetTasks[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);
  }

  // 1. Optimistic Update directly to Redux database
  const isTargetFolder = targetParentType === EntityLayerConst.ProjectFolder;
  store.dispatch(taskSlice.actions.upsert({
    id: activeData.id,
    projectFolderId: isTargetFolder ? targetParentId : undefined,
    projectSpaceId: isTargetFolder ? state.folders.entities[targetParentId]?.parentId : targetParentId,
    parentType: targetParentType,
    orderKey: newOrderKey
  }));

  // 2. Trigger RTK Query mutation (rollback fully handled on request failure)
  moveItemMutation({
    workspaceId,
    body: {
      itemId: activeData.id,
      itemType: EntityLayerConst.ProjectTask,
      targetParentId: targetParentId,
      targetParentType: targetParentType,
      nextItemOrderKey: nextKey,
      newOrderKey,
      sourceParentId: sourceParentId,
      sourceParentType: activeData.parentType
    }
  });
}

import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst, EntityLayerType } from "@/types/entity-layer-type";
import { fractionalBetween } from "../../utils/fractional-index";
import { useHierarchyStore } from "../../use-hierarchy-store";
import type { MoveItemRequest } from "../../hierarchy-api";
import type { DragItemData, DragTaskData } from "../drag-item-type";

export function handleTaskMove(
  activeData: DragTaskData,
  overData: DragItemData,
  mutateMoveItem: (req: MoveItemRequest, options?: { onError?: () => void }) => void
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

  const store = useHierarchyStore.getState();
  const snapshot = {
    tasksByParent: store.tasksByParent,
    tasks: store.tasks,
    spaces: store.spaces,
    folders: store.folders
  };

  const sourceParentId = activeData.parentId;
  
  const sourceTasks = store.tasksByParent[sourceParentId] || [];
  const targetTasks = sourceParentId === targetParentId 
    ? sourceTasks 
    : (store.tasksByParent[targetParentId] || []);

  const oldIndex = sourceTasks.indexOf(activeData.id);
  let newIndex = targetTasks.indexOf(overData.id);
  if (newIndex === -1) newIndex = 0;

  let prevKey: string | undefined;
  let nextKey: string | undefined;
  let newOrderKey: string | undefined;

  if (sourceParentId === targetParentId) {
    if (oldIndex === -1 || newIndex === -1) return;
    const moved = arrayMove(sourceTasks, oldIndex, newIndex);
    
    prevKey = newIndex > 0 ? store.tasks[moved[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < moved.length - 1 ? store.tasks[moved[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);

    const newTasks = { ...store.tasks };
    if (newTasks[activeData.id]) {
      newTasks[activeData.id] = {
        ...newTasks[activeData.id],
        orderKey: newOrderKey || newTasks[activeData.id].orderKey
      };
    }

    useHierarchyStore.setState({
      tasksByParent: { ...store.tasksByParent, [sourceParentId]: moved },
      tasks: newTasks
    });
  } else {
    const newSourceTasks = sourceTasks.filter(id => id !== activeData.id);
    const newTargetTasks = [...targetTasks];
    newTargetTasks.splice(newIndex, 0, activeData.id);

    prevKey = newIndex > 0 ? store.tasks[newTargetTasks[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < newTargetTasks.length - 1 ? store.tasks[newTargetTasks[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);

    const newTasks = { ...store.tasks };
    if (newTasks[activeData.id]) {
      const isTargetFolder = targetParentType === EntityLayerConst.ProjectFolder;
      newTasks[activeData.id] = {
        ...newTasks[activeData.id],
        projectFolderId: isTargetFolder ? targetParentId : undefined,
        projectSpaceId: isTargetFolder ? store.folders[targetParentId]?.parentId : targetParentId,
        parentType: targetParentType,
        orderKey: newOrderKey || newTasks[activeData.id].orderKey
      };
    }

    const newSpaces = { ...store.spaces };
    const newFolders = { ...store.folders };

    // Update hasTasks for source parent
    if (newSourceTasks.length === 0) {
      if (newSpaces[sourceParentId]) newSpaces[sourceParentId] = { ...newSpaces[sourceParentId], hasTasks: false };
      if (newFolders[sourceParentId]) newFolders[sourceParentId] = { ...newFolders[sourceParentId], hasTasks: false };
    }

    // Update hasTasks for target parent
    if (newTargetTasks.length > 0) {
      if (newSpaces[targetParentId]) newSpaces[targetParentId] = { ...newSpaces[targetParentId], hasTasks: true };
      if (newFolders[targetParentId]) newFolders[targetParentId] = { ...newFolders[targetParentId], hasTasks: true };
    }

    useHierarchyStore.setState({
      tasksByParent: { 
        ...store.tasksByParent, 
        [sourceParentId]: newSourceTasks,
        [targetParentId]: newTargetTasks
      },
      spaces: newSpaces,
      folders: newFolders,
      tasks: newTasks
    });
  }

  mutateMoveItem({
    itemId: activeData.id,
    itemType: EntityLayerConst.ProjectTask,
    targetParentId: targetParentId,
    targetParentType: targetParentType,
    nextItemOrderKey: nextKey,
    newOrderKey,
    sourceParentId: sourceParentId,
    sourceParentType: activeData.parentType
  }, {
    onError: () => {
      // Revert optimistic update if API fails
      useHierarchyStore.setState(snapshot);
    }
  });
}

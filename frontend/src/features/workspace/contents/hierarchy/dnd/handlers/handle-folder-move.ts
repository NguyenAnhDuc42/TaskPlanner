import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { fractionalBetween } from "../../utils/fractional-index";
import { useHierarchyStore } from "../../use-hierarchy-store";
import type { MoveItemRequest } from "../../hierarchy-api";
import type { DragItemData, DragFolderData } from "../drag-item-type";

export function handleFolderMove(
  activeData: DragFolderData,
  overData: DragItemData,
  mutateMoveItem: (req: MoveItemRequest) => void
) {
  let targetSpaceId: string | undefined;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId; // FolderRecord has spaceId injected in SortableItem data
  }

  if (!targetSpaceId) return;

  const store = useHierarchyStore.getState();
  const sourceSpaceId = activeData.spaceId || activeData.parentId;
  
  const sourceFolders = store.foldersBySpace[sourceSpaceId] || [];
  const targetFolders = sourceSpaceId === targetSpaceId 
    ? sourceFolders 
    : (store.foldersBySpace[targetSpaceId] || []);

  const oldIndex = sourceFolders.indexOf(activeData.id);
  let newIndex = targetFolders.indexOf(overData.id);
  if (newIndex === -1) newIndex = 0; // If dropped on empty space

  let prevKey: string | undefined;
  let nextKey: string | undefined;
  let newOrderKey: string | undefined;

  if (sourceSpaceId === targetSpaceId) {
    if (oldIndex === -1 || newIndex === -1) return;
    const moved = arrayMove(sourceFolders, oldIndex, newIndex);
    
    prevKey = newIndex > 0 ? store.folders[moved[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < moved.length - 1 ? store.folders[moved[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);

    const newFoldersStore = { ...store.folders };
    if (newFoldersStore[activeData.id]) {
      newFoldersStore[activeData.id] = {
        ...newFoldersStore[activeData.id],
        orderKey: newOrderKey || newFoldersStore[activeData.id].orderKey
      };
    }

    useHierarchyStore.setState({
      foldersBySpace: { ...store.foldersBySpace, [sourceSpaceId]: moved },
      folders: newFoldersStore
    });
  } else {
    // Moving across spaces
    const newSourceFolders = sourceFolders.filter(id => id !== activeData.id);
    const newTargetFolders = [...targetFolders];
    newTargetFolders.splice(newIndex, 0, activeData.id);

    prevKey = newIndex > 0 ? store.folders[newTargetFolders[newIndex - 1]]?.orderKey : undefined;
    nextKey = newIndex < newTargetFolders.length - 1 ? store.folders[newTargetFolders[newIndex + 1]]?.orderKey : undefined;
    newOrderKey = fractionalBetween(prevKey, nextKey);

    const newFoldersStore = { ...store.folders };
    if (newFoldersStore[activeData.id]) {
      newFoldersStore[activeData.id] = {
        ...newFoldersStore[activeData.id],
        parentId: targetSpaceId,
        orderKey: newOrderKey || newFoldersStore[activeData.id].orderKey
      };
    }

    useHierarchyStore.setState({
      foldersBySpace: { 
        ...store.foldersBySpace, 
        [sourceSpaceId]: newSourceFolders,
        [targetSpaceId]: newTargetFolders
      },
      folders: newFoldersStore
    });
  }

  mutateMoveItem({
    itemId: activeData.id,
    itemType: EntityLayerConst.ProjectFolder,
    targetParentId: targetSpaceId,
    targetParentType: EntityLayerConst.ProjectSpace,
    nextItemOrderKey: nextKey,
    newOrderKey,
    sourceParentId: sourceSpaceId,
    sourceParentType: EntityLayerConst.ProjectSpace
  });
}

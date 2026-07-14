import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { DragItemData, DragFolderData } from "../drag-item-type";
import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import type { FolderMutations } from "@/mutations/folder.mutations";
import type { TaskMutations } from "@/mutations/task.mutations";

export function handleFolderMove(
  rootStore: WorkspaceRootStore,
  folderMutations: FolderMutations,
  taskMutations: TaskMutations,
  activeData: DragFolderData,
  overData: DragItemData,
): void {
  let targetSpaceId: string | undefined;

  if (overData?.type === EntityLayerConst.ProjectSpace) {
    targetSpaceId = overData.id;
  } else if (overData?.type === EntityLayerConst.ProjectFolder) {
    targetSpaceId = overData.spaceId;
  }

  if (!targetSpaceId) return;

  const sourceSpaceId = activeData.spaceId;

  // getBySpace is already orderKey-sorted (cached computed index — do not sort in place).
  const sourceFoldersList = rootStore.folderStore.getBySpace(sourceSpaceId);
  const sourceFolders = sourceFoldersList.map(f => f.id);

  const targetFoldersList = sourceSpaceId === targetSpaceId
    ? sourceFoldersList
    : rootStore.folderStore.getBySpace(targetSpaceId);
  const targetFolders = targetFoldersList.map(f => f.id);

  // Guard: dropping onto itself or its current parent space header
  if (activeData.id === overData.id || overData.id === activeData.spaceId) {
    return;
  }

  const oldIndex = sourceFolders.indexOf(activeData.id);
  let newIndex = targetFolders.indexOf(overData.id);

  // If dropped on container space header, append to the end
  if (newIndex === -1) newIndex = targetFolders.length;

  if (sourceSpaceId === targetSpaceId && oldIndex === newIndex) {
    return;
  }

  let newOrderKey: string;
  const patch: { orderKey: string; spaceId?: string } = { orderKey: "" };

  if (sourceSpaceId === targetSpaceId) {
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceFoldersList, oldIndex, newIndex);
    const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
    const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
    newOrderKey = fractionalBetween(prevKey, nextKey);
    patch.orderKey = newOrderKey;
  } else {
    // Cross-space move: insert at newIndex in target list
    const prevKey = safeKey(targetFoldersList[newIndex - 1]?.orderKey);
    const nextKey = safeKey(targetFoldersList[newIndex]?.orderKey);
    newOrderKey = fractionalBetween(prevKey, nextKey);
    patch.orderKey = newOrderKey;
    patch.spaceId = targetSpaceId;
  }

  folderMutations.updateLocal(activeData.id, patch).catch((err) => console.error("Failed to move folder", err));

  // cascade for the same reason.
  if (patch.spaceId) {
    const childTasks = [...rootStore.taskStore.getByFolder(activeData.id)];
    for (const task of childTasks) {
      taskMutations.updateLocal(task.id, { spaceId: patch.spaceId }).catch((err) => console.error("Failed to cascade folder move to task", err));
    }
  }
}

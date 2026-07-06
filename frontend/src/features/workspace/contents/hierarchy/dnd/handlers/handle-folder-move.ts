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

  const sourceFoldersList = rootStore.folderStore.getBySpace(sourceSpaceId)
    .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
  const sourceFolders = sourceFoldersList.map(f => f.id);

  const targetFoldersList = sourceSpaceId === targetSpaceId
    ? sourceFoldersList
    : rootStore.folderStore.getBySpace(targetSpaceId).sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
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

  // Cascade: a folder moving to a different space takes its tasks with it — otherwise they
  // silently keep referencing the old space (invisible on the new space's board, and wrongly
  // swept up if the old space gets deleted later). Mirrors the backend's UpdateFolderHandler
  // cascade for the same reason.
  if (patch.spaceId) {
    const childTasks = rootStore.taskStore.all.filter((t) => t.folderId === activeData.id);
    for (const task of childTasks) {
      taskMutations.updateLocal(task.id, { spaceId: patch.spaceId }).catch((err) => console.error("Failed to cascade folder move to task", err));
    }
  }
}

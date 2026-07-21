import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { DragItemData, DragDocumentData } from "../drag-item-type";
import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import type { DocumentMutations } from "@/mutations/document.mutations";

// "before"/"after" reorder as a sibling relative to the row dropped on; "nest" makes the dragged
// document a child of the row dropped on instead. Derived from where within the target row's own
// height the drag was released — see computeDropZone in use-hierarchy-dnd.ts.
export type DropZone = "before" | "nest" | "after";

export function handleDocumentMove(
  rootStore: WorkspaceRootStore,
  documentMutations: DocumentMutations,
  activeData: DragDocumentData,
  overData: DragItemData,
  dropZone: DropZone,
): void {
  if (overData?.type !== EntityLayerConst.ProjectDocument) return;
  if (activeData.spaceId !== overData.spaceId) return;
  if (activeData.id === overData.id) return;

  const descendantIds = rootStore.documentStore.getDescendantIds(activeData.id);
  // Cycle guard — no existing fixed-depth DnD handler needs this, since Space/Folder/Task never
  // nest into their own kind. A document can't be dropped into any group rooted at its own subtree,
  // nor nested directly under itself.
  if (descendantIds.includes(overData.id)) return;

  if (dropZone === "nest") {
    const targetChildren = rootStore.documentStore.getChildren(overData.id);
    const lastKey = safeKey(targetChildren[targetChildren.length - 1]?.orderKey);
    documentMutations
      .updateLocal(activeData.id, { orderKey: fractionalBetween(lastKey, null), parentDocumentId: overData.id })
      .catch((err) => console.error("Failed to nest document", err));
    return;
  }

  const targetParentId = overData.parentId ?? null;
  if (targetParentId && descendantIds.includes(targetParentId)) return;

  const sourceParentId = activeData.parentId ?? null;

  const sourceSiblingsList = sourceParentId
    ? rootStore.documentStore.getChildren(sourceParentId)
    : rootStore.documentStore.getRootsBySpace(activeData.spaceId);
  const sourceSiblings = sourceSiblingsList.map((d) => d.id);

  const targetSiblingsList = sourceParentId === targetParentId
    ? sourceSiblingsList
    : targetParentId
      ? rootStore.documentStore.getChildren(targetParentId)
      : rootStore.documentStore.getRootsBySpace(activeData.spaceId);
  const targetSiblings = targetSiblingsList.map((d) => d.id);

  const oldIndex = sourceSiblings.indexOf(activeData.id);
  let newIndex = targetSiblings.indexOf(overData.id);
  if (newIndex === -1) newIndex = targetSiblings.length;
  else if (dropZone === "after") newIndex += 1;

  if (sourceParentId === targetParentId && oldIndex === newIndex) return;
  // Same adjustment arrayMove itself does internally when moving forward within one array — the
  // target's own index shifts down by one once the source item is spliced out ahead of it.
  if (sourceParentId === targetParentId && oldIndex < newIndex) newIndex -= 1;

  const patch: { orderKey: string; parentDocumentId?: string; clearParent?: boolean } = { orderKey: "" };

  if (sourceParentId === targetParentId) {
    if (oldIndex === -1) return;
    const moved = arrayMove(sourceSiblingsList, oldIndex, newIndex);
    const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
    const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
    patch.orderKey = fractionalBetween(prevKey, nextKey);
  } else {
    const prevKey = safeKey(targetSiblingsList[newIndex - 1]?.orderKey);
    const nextKey = safeKey(targetSiblingsList[newIndex]?.orderKey);
    patch.orderKey = fractionalBetween(prevKey, nextKey);
    if (targetParentId) patch.parentDocumentId = targetParentId;
    else patch.clearParent = true;
  }

  documentMutations.updateLocal(activeData.id, patch).catch((err) => console.error("Failed to move document", err));
}

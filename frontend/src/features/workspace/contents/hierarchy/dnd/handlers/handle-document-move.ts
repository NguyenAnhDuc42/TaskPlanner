import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { DragItemData, DragDocumentData } from "../drag-item-type";
import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import type { DocumentMutations } from "@/mutations/document.mutations";

// Dropping a document onto another document reorders it as a SIBLING within that target
// document's own parent group (mirroring Folder's "drop onto X joins X's container" model) —
// it does not nest the dragged document as a child of the node it's dropped on. Nesting is done
// explicitly via the "New sub-page" context menu action instead, avoiding the extra drop-zone
// affordance a Notion-style "drop onto vs. drop between" gesture would require.
export function handleDocumentMove(
  rootStore: WorkspaceRootStore,
  documentMutations: DocumentMutations,
  activeData: DragDocumentData,
  overData: DragItemData,
): void {
  if (overData?.type !== EntityLayerConst.ProjectDocument) return;
  if (activeData.spaceId !== overData.spaceId) return;
  if (activeData.id === overData.id) return;

  const targetParentId = overData.parentId ?? null;

  // Cycle guard — no existing fixed-depth DnD handler needs this, since Space/Folder/Task never
  // nest into their own kind. A document can't be dropped into any group rooted at its own subtree.
  if (targetParentId) {
    const descendantIds = rootStore.documentStore.getDescendantIds(activeData.id);
    if (descendantIds.includes(targetParentId)) return;
  }

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

  if (sourceParentId === targetParentId && oldIndex === newIndex) return;

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

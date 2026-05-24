import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";

import type { DragItemData, DragSpaceData } from "../drag-item-type";
import { useHierarchyStore } from "../../use-hierarchy-store";
import type { MoveItemRequest } from "../../hierarchy-api";
import { fractionalBetween } from "../../utils/fractional-index";

export function handleSpaceMove(
  workspaceId: string,
  activeData: DragSpaceData,
  overData: DragItemData,
  mutateMoveItem: (req: MoveItemRequest) => void
) {
  if (overData?.type !== EntityLayerConst.ProjectSpace) return;

  const store = useHierarchyStore.getState();
  const rootSpaceIds = store.rootSpaceIds;
  const oldIndex = rootSpaceIds.indexOf(activeData.id);
  const newIndex = rootSpaceIds.indexOf(overData.id);
  
  if (oldIndex === -1 || newIndex === -1) return;

  const moved = arrayMove(rootSpaceIds, oldIndex, newIndex);
  
  const spaces = store.spaces;
  const prevKey = newIndex > 0 ? spaces[moved[newIndex - 1]]?.orderKey : undefined;
  const nextKey = newIndex < moved.length - 1 ? spaces[moved[newIndex + 1]]?.orderKey : undefined;
  const newOrderKey = fractionalBetween(prevKey, nextKey);
  
  // Optimistic UI update via Zustand
  useHierarchyStore.setState({ rootSpaceIds: moved });

  mutateMoveItem({
    itemId: activeData.id,
    itemType: EntityLayerConst.ProjectSpace,
    targetParentId: workspaceId,
    targetParentType: "Workspace",
    nextItemOrderKey: nextKey,
    newOrderKey
  });
}

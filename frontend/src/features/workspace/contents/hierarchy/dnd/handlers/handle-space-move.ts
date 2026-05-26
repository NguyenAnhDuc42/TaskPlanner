import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { DragItemData, DragSpaceData } from "../drag-item-type";
import type { MoveItemRequest } from "../../hierarchy-api";
import { fractionalBetween } from "../../utils/fractional-index";
import { store } from "@/store";
import { spaceSlice } from "@/store/entityStore";

export function handleSpaceMove(
  workspaceId: string,
  activeData: DragSpaceData,
  overData: DragItemData,
  moveItemMutation: (args: { workspaceId: string; body: MoveItemRequest }) => Promise<unknown>
) {
  if (overData?.type !== EntityLayerConst.ProjectSpace) return;

  const state = store.getState();
  const spacesList = Object.values(state.spaces.entities)
    .filter((s): s is typeof s & { id: string } => !!s)
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));

  const rootSpaceIds = spacesList.map(s => s.id);
  const oldIndex = rootSpaceIds.indexOf(activeData.id);
  const newIndex = rootSpaceIds.indexOf(overData.id);
  
  if (oldIndex === -1 || newIndex === -1) return;

  const moved = arrayMove(rootSpaceIds, oldIndex, newIndex);
  
  const prevKey = newIndex > 0 ? state.spaces.entities[moved[newIndex - 1]]?.orderKey : undefined;
  const nextKey = newIndex < moved.length - 1 ? state.spaces.entities[moved[newIndex + 1]]?.orderKey : undefined;
  const newOrderKey = fractionalBetween(prevKey, nextKey);
  
  // 1. Optimistic Update directly to Redux database!
  store.dispatch(spaceSlice.actions.upsert({ id: activeData.id, orderKey: newOrderKey }));

  // 2. Trigger RTK Query mutation (automatically handles fallback/rollback on failure)
  moveItemMutation({
    workspaceId,
    body: {
      itemId: activeData.id,
      itemType: EntityLayerConst.ProjectSpace,
      targetParentId: workspaceId,
      targetParentType: "Workspace",
      nextItemOrderKey: nextKey,
      newOrderKey
    }
  });
}

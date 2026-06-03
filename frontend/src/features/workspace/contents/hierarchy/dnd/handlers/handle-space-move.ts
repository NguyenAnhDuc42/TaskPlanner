import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { DragItemData, DragSpaceData } from "../drag-item-type";
import { generateKeyBetween } from "fractional-indexing";
import { safeKey } from "../../utils/fractional-index";
import { store } from "@/store";
import { spaceSlice } from "@/store/entityStore";

export function handleSpaceMove(
  activeData: DragSpaceData,
  overData: DragItemData,
  triggerBatchMove: (move: { kind: "space"; itemId: string; newOrderKey: string }) => void
) {
  if (overData?.type !== EntityLayerConst.ProjectSpace) return;

  const state = store.getState();
  const spacesList = Object.values(state.spaces.entities)
    .filter((s): s is typeof s & { id: string } => !!s)
    .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));

  const oldIndex = spacesList.findIndex(s => s.id === activeData.id);
  const newIndex = spacesList.findIndex(s => s.id === overData.id);

  if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;

  const moved = arrayMove(spacesList, oldIndex, newIndex);

  const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
  const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
  const newOrderKey = generateKeyBetween(prevKey, nextKey);

  // 1. Optimistic update (only update the dragged space)
  store.dispatch(spaceSlice.actions.upsert({ id: activeData.id, orderKey: newOrderKey }));

  // 2. Trigger batch queue
  triggerBatchMove({ kind: "space", itemId: activeData.id, newOrderKey });
}

import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { DragItemData, DragSpaceData } from "../drag-item-type";
import { generateNKeysBetween } from "fractional-indexing";
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

  const rootSpaceIds = spacesList.map(s => s.id);
  const oldIndex = rootSpaceIds.indexOf(activeData.id);
  const newIndex = rootSpaceIds.indexOf(overData.id);

  if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;

  const moved = arrayMove(rootSpaceIds, oldIndex, newIndex);

  // Generate fresh rocicorp keys for ALL spaces so mixed numeric/"a0" keys
  // don't break the sort order. Only the dragged space's key goes to the server.
  const freshKeys = generateNKeysBetween(null, null, moved.length);
  const newOrderKey = freshKeys[newIndex];

  // 1. Optimistic update — update ALL spaces so the sort stays consistent
  moved.forEach((id, i) => {
    store.dispatch(spaceSlice.actions.upsert({ id, orderKey: freshKeys[i] }));
  });

  // 2. Trigger batch queue (only send the dragged item's new key to server)
  triggerBatchMove({ kind: "space", itemId: activeData.id, newOrderKey });
}

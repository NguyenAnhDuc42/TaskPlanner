import { arrayMove } from "@dnd-kit/sortable";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { DragItemData, DragSpaceData } from "../drag-item-type";
import { safeKey, fractionalBetween } from "../../utils/fractional-index";
import type { RootStore } from "@/stores/root.store";
import type { SpaceMutations } from "@/mutations/space.mutations";

export function handleSpaceMove(
  rootStore: RootStore,
  spaceMutations: SpaceMutations,
  activeData: DragSpaceData,
  overData: DragItemData,
): void {
  if (overData?.type !== EntityLayerConst.ProjectSpace) return;

  const spacesList = rootStore.spaceStore.all
    .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const oldIndex = spacesList.findIndex(s => s.id === activeData.id);
  const newIndex = spacesList.findIndex(s => s.id === overData.id);

  if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;

  const moved = arrayMove(spacesList, oldIndex, newIndex);

  const prevKey = safeKey(moved[newIndex - 1]?.orderKey);
  const nextKey = safeKey(moved[newIndex + 1]?.orderKey);
  const newOrderKey = fractionalBetween(prevKey, nextKey);

  spaceMutations.updateLocal(activeData.id, { orderKey: newOrderKey }).catch((err) => console.error("Failed to reorder space", err));
}

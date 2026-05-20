import { getPriorityWeight, Priority } from "@/types/priority";
import type { LayerItem } from "../../layer-detail-types";

function toPriority(value: unknown): Priority {
  if (value === Priority.Low) return Priority.Low;
  if (value === Priority.Normal) return Priority.Normal;
  if (value === Priority.High) return Priority.High;
  if (value === Priority.Urgent) return Priority.Urgent;
  return Priority.Low;
}

/**
 * Infers the priority for a moved item based on its would-be neighbors in the destination list.
 * - Looks at the item above; if none, uses the item below; if none, falls back to the active item.
 */
export function inferPriorityFromNeighbors(args: {
  activeItem: LayerItem;
  targetIndex: number;
  dstItems: LayerItem[];
}): Priority {
  const { activeItem, targetIndex, dstItems } = args;

  const stripped = dstItems.filter((item) => item.id !== activeItem.id);
  const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
  const prev = stripped[clampedIndex - 1];
  const next = stripped[clampedIndex];

  const activePriority = toPriority(activeItem.priority);
  const activeWeight = getPriorityWeight({ priority: activePriority });

  // If only one neighbor exists, inherit its priority
  if (prev && !next) return toPriority(prev.priority);
  if (next && !prev) {
    const nextPriority = toPriority(next.priority);
    const nextWeight = getPriorityWeight({ priority: nextPriority });
    // Dropping at the top of a column: don't downgrade just because the first item is lower.
    return nextWeight < activeWeight ? activePriority : nextPriority;
  }
  if (!prev && !next) return activePriority;

  // Both neighbors exist: apply the rule set described
  const prevPriority = toPriority(prev?.priority);
  const nextPriority = toPriority(next?.priority);
  const prevWeight = getPriorityWeight({ priority: prevPriority });
  const nextWeight = getPriorityWeight({ priority: nextPriority });

  // If the item above is lower, always take that lower priority (downgrade).
  if (prevWeight < activeWeight) return prevPriority;

  // If the item above is higher:
  // - if below is same as active, take below (no change)
  // - if below is lower, keep active (don't downgrade because of below)
  // - if below is higher, upgrade to below
  if (prevWeight > activeWeight) {
    if (nextWeight === activeWeight) return activePriority;
    if (nextWeight < activeWeight) return activePriority;
    return nextPriority;
  }

  // If the item above is same as active, keep active.
  return activePriority;
}

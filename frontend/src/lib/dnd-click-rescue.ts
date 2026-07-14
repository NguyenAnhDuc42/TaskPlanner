import type { DragEndEvent } from "@dnd-kit/core";

const CLICK_RESCUE_MAX_DISTANCE_PX = 10;

export function rescueSwallowedClick(event: DragEndEvent): void {
  const { active, over, delta, activatorEvent } = event;

  // Only a no-op drop qualifies — anything landing on another droppable was a real drag.
  if (over && over.id !== active.id) return;
  if (Math.hypot(delta.x, delta.y) >= CLICK_RESCUE_MAX_DISTANCE_PX) return;

  // Keyboard-initiated drags (Enter/Space) have no click to rescue.
  if (!(activatorEvent instanceof PointerEvent) && !(activatorEvent instanceof MouseEvent)) return;

  const target = activatorEvent.target;
  if (target instanceof HTMLElement && target.isConnected) {
    target.click();
  }
}

import type { DragEndEvent } from "@dnd-kit/core";

const CLICK_RESCUE_MAX_DISTANCE_PX = 10;

export function rescueSwallowedClick(event: DragEndEvent): void {
  const { active, over, delta, activatorEvent } = event;

  if (over && over.id !== active.id) return;
  if (Math.hypot(delta.x, delta.y) >= CLICK_RESCUE_MAX_DISTANCE_PX) return;

  if (!(activatorEvent instanceof PointerEvent) && !(activatorEvent instanceof MouseEvent)) return;

  const target = activatorEvent.target;
  if (target instanceof HTMLElement && target.isConnected) {
    target.click();
  }
}

import { closestCenter, pointerWithin, type CollisionDetection } from "@dnd-kit/core";

// closestCenter picks whichever droppable's CENTER POINT is geometrically nearest — it doesn't
// care whether your cursor is actually over that droppable at all. For a dense list (nested tree
// rows, or several short rows stacked tightly), that means dropping "on" an item can register as
// "over" a neighboring row whose center just happens to be closer, landing the item somewhere
// other than where you visually released it. pointerWithin only matches a droppable whose actual
// bounding rect contains the pointer, which matches user expectation ("drop where I let go").
// Falls back to closestCenter when the pointer isn't over anything at all (e.g. dragging past the
// last item, into empty space below the list) — pointerWithin returns nothing there.
export const pointerAwareCollisionDetection: CollisionDetection = (args) => {
  const pointerCollisions = pointerWithin(args);
  if (pointerCollisions.length > 0) return pointerCollisions;
  return closestCenter(args);
};

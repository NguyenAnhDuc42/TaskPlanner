import { useEffect } from "react";

export function useSmartWheelScroll(
  containerRef: React.RefObject<HTMLElement | null>,
  isDragging: boolean
) {
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const handleWheel = (e: WheelEvent) => {
      // If actively dragging, do not interfere with wheel scroll
      if (isDragging) return;

      // Find if we are scrolling inside a vertically scrollable column container
      const scrollableColumn = (e.target as HTMLElement).closest(".status-column-scrollable");

      // If we are NOT hovering over a scrollable column (i.e. we are on the board background),
      // we translate vertical scroll (deltaY) into horizontal scroll of the board.
      if (!scrollableColumn) {
        if (e.deltaY !== 0) {
          e.preventDefault();
          el.scrollLeft += e.deltaY;
        }
      }
    };

    el.addEventListener("wheel", handleWheel, { passive: false });
    return () => {
      el.removeEventListener("wheel", handleWheel);
    };
  }, [containerRef, isDragging]);
}

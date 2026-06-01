import { useEffect, useRef } from "react";

export function useEdgeScroll(
  containerRef: React.RefObject<HTMLElement | null>,
  isDragging: boolean,
  isListMode?: boolean // Supports vertical list container scrolling
) {
  const mousePosRef = useRef<{ x: number; y: number } | null>(null);

  useEffect(() => {
    const handlePointerMove = (e: PointerEvent) => {
      mousePosRef.current = { x: e.clientX, y: e.clientY };
    };
    window.addEventListener("pointermove", handlePointerMove);
    return () => window.removeEventListener("pointermove", handlePointerMove);
  }, []);

  useEffect(() => {
    if (!isDragging) {
      mousePosRef.current = null;
      return;
    }

    let frameId: number;
    const checkScroll = () => {
      const el = containerRef.current;
      if (!el) return;

      if (!mousePosRef.current) {
        frameId = requestAnimationFrame(checkScroll);
        return;
      }

      const rect = el.getBoundingClientRect();
      const { x, y } = mousePosRef.current;
      const baseSpeed = 120;
      const threshold = 120;

      if (isListMode) {
        // We use a larger threshold at the bottom (160px) to clear any footer overlay!
        const topThreshold = 120;
        const bottomThreshold = 160;

        if (y < rect.top + topThreshold) {
          const intensity = Math.min(1, (rect.top + topThreshold - y) / topThreshold);
          const speed = Math.max(45, Math.ceil(baseSpeed * intensity));
          el.scrollTop -= speed;
        } else if (y > rect.bottom - bottomThreshold) {
          const intensity = Math.min(1, (y - (rect.bottom - bottomThreshold)) / bottomThreshold);
          const speed = Math.max(45, Math.ceil(baseSpeed * intensity));
          el.scrollTop += speed;
        }
      } else {
        // Horizontal scrolling for the main board viewport
        if (x < rect.left + threshold) {
          const intensity = Math.min(1, (rect.left + threshold - x) / threshold);
          el.scrollLeft -= Math.ceil(baseSpeed * intensity);
        } else if (x > rect.right - threshold) {
          const intensity = Math.min(1, (x - (rect.right - threshold)) / threshold);
          el.scrollLeft += Math.ceil(baseSpeed * intensity);
        }

        // Context-aware vertical scrolling for the column currently hovered
        const column = document.elementFromPoint(x, y)?.closest(".status-column-scrollable");
        if (column) {
          const colRect = column.getBoundingClientRect();
          if (y < colRect.top + threshold) {
            const intensity = Math.min(1, (colRect.top + threshold - y) / threshold);
            column.scrollTop -= Math.ceil(baseSpeed * intensity);
          } else if (y > colRect.bottom - threshold) {
            const intensity = Math.min(1, (y - (colRect.bottom - threshold)) / threshold);
            column.scrollTop += Math.ceil(baseSpeed * intensity);
          }
        }
      }

      frameId = requestAnimationFrame(checkScroll);
    };

    frameId = requestAnimationFrame(checkScroll);
    return () => cancelAnimationFrame(frameId);
  }, [isDragging, containerRef, isListMode]);
}

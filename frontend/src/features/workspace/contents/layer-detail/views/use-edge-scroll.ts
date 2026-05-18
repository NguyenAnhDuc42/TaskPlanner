import { useEffect, useRef } from "react";

export function useEdgeScroll(
  containerRef: React.RefObject<HTMLElement | null>,
  isDragging: boolean,
  isListMode?: boolean // Supports vertical list container scrolling
) {
  const mousePosRef = useRef({ x: 0, y: 0 });

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      mousePosRef.current = { x: e.clientX, y: e.clientY };
    };
    window.addEventListener("mousemove", handleMouseMove);
    return () => window.removeEventListener("mousemove", handleMouseMove);
  }, []);

  useEffect(() => {
    if (!isDragging) return;

    let frameId: number;
    const checkScroll = () => {
      const el = containerRef.current;
      if (!el) return;

      const rect = el.getBoundingClientRect();
      const { x, y } = mousePosRef.current;
      const threshold = 100;
      const speed = 15;

      if (isListMode) {
        // Vertical-only scrolling for the list container
        if (y < rect.top + threshold) {
          el.scrollTop -= speed;
        } else if (y > rect.bottom - threshold) {
          el.scrollTop += speed;
        }
      } else {
        // Horizontal scrolling for the main board viewport
        if (x < rect.left + threshold) {
          el.scrollLeft -= speed;
        } else if (x > rect.right - threshold) {
          el.scrollLeft += speed;
        }

        // Context-aware vertical scrolling for the column currently hovered
        const column = document.elementFromPoint(x, y)?.closest(".status-column-scrollable");
        if (column) {
          const colRect = column.getBoundingClientRect();
          if (y < colRect.top + threshold) {
            column.scrollTop -= speed;
          } else if (y > colRect.bottom - threshold) {
            column.scrollTop += speed;
          }
        }
      }

      frameId = requestAnimationFrame(checkScroll);
    };

    frameId = requestAnimationFrame(checkScroll);
    return () => cancelAnimationFrame(frameId);
  }, [isDragging, containerRef, isListMode]);
}

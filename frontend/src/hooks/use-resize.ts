import { useState, useCallback, useEffect } from "react";

interface UseResizeOptions {
  initialWidth: number;
  minWidth: number;
  maxWidth: number;
  direction: "left" | "right";
  offset?: number;
  onResizeEnd?: (newWidth: number) => void;
}

export function useResize({
  initialWidth,
  minWidth,
  maxWidth,
  direction,
  offset = 0,
  onResizeEnd,
}: UseResizeOptions) {
  const [width, setWidth] = useState(initialWidth);
  const [isResizing, setIsResizing] = useState(false);

  const startResizing = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      setIsResizing(true);

      const startX = e.clientX;
      const startWidth = width;

      const onMouseMove = (moveEvent: MouseEvent) => {
        let delta: number;
        if (direction === "left") {
          delta = moveEvent.clientX - startX;
        } else {
          delta = startX - moveEvent.clientX;
        }
        const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidth + delta));
        setWidth(newWidth);
      };

      const onMouseUp = () => {
        setIsResizing(false);
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
        document.body.style.cursor = "";
        document.body.style.userSelect = "";
        // Get the final width from state via closure
        setWidth((finalWidth) => {
          onResizeEnd?.(finalWidth);
          return finalWidth;
        });
      };

      document.addEventListener("mousemove", onMouseMove);
      document.addEventListener("mouseup", onMouseUp);
      document.body.style.cursor = "col-resize";
      document.body.style.userSelect = "none";
    },
    [width, minWidth, maxWidth, direction, onResizeEnd],
  );

  return { width, isResizing, startResizing };
}

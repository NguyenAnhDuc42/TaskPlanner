import { useState, useCallback, useRef, useEffect } from "react";

interface UseResizeOptions {
  initialWidth: number;
  minWidth: number;
  maxWidth: number;
  direction: "left" | "right";
  onResize?: (newWidth: number) => void;
  onResizeEnd?: (newWidth: number) => void;
}

export function useResize({
  initialWidth,
  minWidth,
  maxWidth,
  direction,
  onResize,
  onResizeEnd,
}: UseResizeOptions) {
  const [width, setWidth] = useState(initialWidth);
  const [isResizing, setIsResizing] = useState(false);
  
  // Refs to maintain stable values across renders and closures
  const startXRef = useRef<number>(0);
  const startWidthRef = useRef<number>(0);
  const onResizeRef = useRef(onResize);
  const onResizeEndRef = useRef(onResizeEnd);
  const currentWidthRef = useRef(width);

  // Keep refs in sync
  onResizeRef.current = onResize;
  onResizeEndRef.current = onResizeEnd;
  currentWidthRef.current = width;

  // Sync with external initialWidth changes
  useEffect(() => {
    if (!isResizing) {
      setWidth(initialWidth);
    }
  }, [initialWidth, isResizing]);

  const startResizing = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      
      startXRef.current = e.clientX;
      startWidthRef.current = currentWidthRef.current;
      setIsResizing(true);

      const onMouseMove = (moveEvent: MouseEvent) => {
        const delta = direction === "left" 
          ? moveEvent.clientX - startXRef.current 
          : startXRef.current - moveEvent.clientX;

        const newRawWidth = startWidthRef.current + delta;
        
        // Snap logic: 0 if below threshold, otherwise clamped
        const finalWidth = newRawWidth < minWidth * 0.8 
          ? 0 
          : Math.min(maxWidth, Math.max(minWidth, newRawWidth));

        setWidth(finalWidth);
        onResizeRef.current?.(finalWidth);
      };

      const onMouseUp = (upEvent: MouseEvent) => {
        setIsResizing(false);
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
        document.body.style.cursor = "";

        // Calculate absolute final width using the final mouse position
        const finalDelta = direction === "left" 
          ? upEvent.clientX - startXRef.current 
          : startXRef.current - upEvent.clientX;
        
        const finalRaw = startWidthRef.current + finalDelta;
        const finalWidth = finalRaw < minWidth * 0.8 
          ? 0 
          : Math.min(maxWidth, Math.max(minWidth, finalRaw));

        onResizeEndRef.current?.(finalWidth);
      };

      document.addEventListener("mousemove", onMouseMove);
      document.addEventListener("mouseup", onMouseUp);
      document.body.style.cursor = "col-resize";
    },
    [direction, minWidth, maxWidth], // width removed from dependencies to stop listener thrashing
  );

  return { width, isResizing, startResizing };
}
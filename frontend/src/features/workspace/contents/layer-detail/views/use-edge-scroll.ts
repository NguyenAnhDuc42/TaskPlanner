import { useEffect, useRef } from "react";

export function useEdgeScroll(
  containerRef: React.RefObject<HTMLElement | null>,
  isDragging: boolean
) {
  const mousePosRef = useRef({ x: 0 });

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      mousePosRef.current = { x: e.clientX };
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
      const x = mousePosRef.current.x;
      const threshold = 100;
      const speed = 15;

      if (x < rect.left + threshold) {
        el.scrollLeft -= speed;
      } else if (x > rect.right - threshold) {
        el.scrollLeft += speed;
      }

      frameId = requestAnimationFrame(checkScroll);
    };

    frameId = requestAnimationFrame(checkScroll);
    return () => cancelAnimationFrame(frameId);
  }, [isDragging, containerRef]);
}

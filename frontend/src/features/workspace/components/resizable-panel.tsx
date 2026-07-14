import type { ReactNode, MouseEvent } from "react";
import { cn } from "@/lib/utils";

interface ResizablePanelProps {
  width: number;
  isResizing: boolean;
  onResizeStart: (e: MouseEvent) => void;
  handleSide: "left" | "right";
  /** Width/opacity collapse to 0 while staying mounted (used by the sidebar, which stays alive
   * across navigation instead of unmounting — see workspace-layout.tsx). */
  collapsed?: boolean;
  /** Suppress the width/opacity transition for renders where the change wasn't a user drag/toggle
   * (e.g. a route change flipping visibility) so only deliberate interaction animates. */
  animate?: boolean;
  children: ReactNode;
  className?: string;
}

export function ResizablePanel({
  width,
  isResizing,
  onResizeStart,
  handleSide,
  collapsed = false,
  animate = true,
  children,
  className,
}: ResizablePanelProps) {
  return (
    <div
      style={{ width: collapsed ? 0 : width, opacity: collapsed ? 0 : 1 }}
      className={cn(
        "flex flex-col h-full shrink-0 relative overflow-hidden bg-card rounded-md",
        !collapsed && "border border-border shadow-sm",
        !isResizing && animate && "transition-all duration-300 ease-in-out",
        className,
      )}
    >
      {children}

      {!collapsed && (
        <div
          onMouseDown={onResizeStart}
          className={cn(
            "absolute top-0 w-[6px] h-full cursor-col-resize z-50 group touch-none",
            handleSide === "left" ? "-left-[3px]" : "-right-[3px]",
            isResizing && "z-[100]",
          )}
        >
          <div
            className={cn(
              "h-full w-[1.5px] mx-auto transition-colors duration-200",
              isResizing ? "bg-primary" : "group-hover:bg-primary/50 bg-transparent",
            )}
          />
        </div>
      )}
    </div>
  );
}

import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

interface SplitViewProps {
  left: ReactNode;
  right: ReactNode;
  isRightOpen: boolean;
  rightWidth?: string;
  className?: string;
}

export function SplitView({
  left,
  right,
  isRightOpen,
  rightWidth = "320px",
  className,
}: SplitViewProps) {
  return (
    <div className={cn("flex-1 flex overflow-hidden h-full gap-2 ", className)}>
      {/* MAIN COLUMN */}
      <div className="flex-1 flex flex-col min-w-0 h-full bg-background border border-border shadow-sm rounded-md overflow-hidden relative">
        {left}
      </div>

      {/* CONTEXT COLUMN */}
      <div
        className={cn(
          "flex flex-col h-full bg-background border border-border shadow-sm rounded-md transition-all duration-500 ease-[cubic-bezier(0.16,1,0.3,1)] overflow-hidden",
          isRightOpen ? "opacity-100" : "w-0 opacity-0 -mr-2 border-none pointer-events-none"
        )}
        style={{ width: isRightOpen ? rightWidth : 0 }}
      >
        {right}
      </div>
    </div>
  );
}

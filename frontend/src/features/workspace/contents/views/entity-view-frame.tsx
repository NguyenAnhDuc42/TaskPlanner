import * as React from "react";
import { cn } from "@/lib/utils";

interface EntityViewFrameProps extends React.HTMLAttributes<HTMLDivElement> {
  topHeader?: React.ReactNode;
  subHeader?: React.ReactNode;
  children: React.ReactNode;
}

export function EntityViewFrame({
  topHeader,
  subHeader,
  children,
  className,
  ...props
}: EntityViewFrameProps) {
  return (
    <div className={cn("flex flex-col h-full w-full bg-background", className)} {...props}>
      {/* Top Header Row */}
      {topHeader && (
        <div className="flex items-center justify-between h-8 px-4 border-b border-border shrink-0">
          {topHeader}
        </div>
      )}

      {/* Sub Header Row (Optional) */}
      {subHeader && (
        <div className="flex items-center px-4 border-b border-border shrink-0 min-h-8">
          {subHeader}
        </div>
      )}

      {/* Main Content Area */}
      <div className="flex-1 overflow-hidden relative">
        {children}
      </div>
    </div>
  );
}

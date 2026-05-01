import React, { useState, useCallback, useEffect } from "react";
import { cn } from "@/lib/utils";

interface ResizableColumnsLayoutProps {
  outerSidebar: React.ReactNode;
  innerSidebar: React.ReactNode;
  mainContent: React.ReactNode;
  contextPane: React.ReactNode;
  isContextOpen?: boolean;
}

/**
 * A premium 4-column layout with native drag-to-resize functionality.
 * Optimized for high-density feature development workflows.
 */
export function ResizableColumnsLayout({
  outerSidebar,
  innerSidebar,
  mainContent,
  contextPane,
  isContextOpen = true,
}: ResizableColumnsLayoutProps) {
  // Track widths of resizable columns
  const [innerSidebarWidth, setInnerSidebarWidth] = useState(260);
  const [contextPaneWidth, setContextPaneWidth] = useState(400);
  const [isResizingInner, setIsResizingInner] = useState(false);
  const [isResizingContext, setIsResizingContext] = useState(false);

  const startResizingInner = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingInner(true);
  }, []);

  const startResizingContext = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingContext(true);
  }, []);

  const stopResizing = useCallback(() => {
    setIsResizingInner(false);
    setIsResizingContext(false);
  }, []);

  const resize = useCallback(
    (e: MouseEvent) => {
      if (isResizingInner) {
        // Min: 200px, Max: 450px. Accounting for Outer Sidebar (approx 64px)
        const newWidth = Math.min(Math.max(200, e.clientX - 64), 450);
        setInnerSidebarWidth(newWidth);
      }
      if (isResizingContext && isContextOpen) {
        // Min: 320px, Max: 50% of screen
        const newWidth = Math.min(Math.max(320, window.innerWidth - e.clientX), window.innerWidth * 0.5);
        setContextPaneWidth(newWidth);
      }
    },
    [isResizingInner, isResizingContext, isContextOpen]
  );

  useEffect(() => {
    if (isResizingInner || isResizingContext) {
      window.addEventListener("mousemove", resize);
      window.addEventListener("mouseup", stopResizing);
      document.body.style.cursor = "col-resize";
      document.body.style.userSelect = "none"; // Prevent text selection while dragging
    } else {
      window.removeEventListener("mousemove", resize);
      window.removeEventListener("mouseup", stopResizing);
      document.body.style.cursor = "default";
      document.body.style.userSelect = "auto";
    }
    return () => {
      window.removeEventListener("mousemove", resize);
      window.removeEventListener("mouseup", stopResizing);
    };
  }, [isResizingInner, isResizingContext, resize, stopResizing]);

  return (
    <div className="flex h-screen w-full overflow-hidden bg-background p-2 gap-0">
      {/* COLUMN 1: Fixed Outer Sidebar (Workspace Navigation) */}
      <div className="w-14 flex-shrink-0 mr-2 flex flex-col h-full">
        {outerSidebar}
      </div>

      {/* COLUMN 2: Resizable Inner Sidebar (Project Hierarchy) */}
      <div 
        style={{ width: innerSidebarWidth }} 
        className="flex-shrink-0 relative flex flex-col h-full bg-foreground/[0.02] rounded-xl border border-white/[0.03]"
      >
        <div className="flex-1 overflow-hidden">
          {innerSidebar}
        </div>
        
        {/* RESIZE HANDLE */}
        <div
          onMouseDown={startResizingInner}
          className={cn(
            "absolute top-0 -right-1 w-2 h-full cursor-col-resize z-50 transition-all duration-300 group",
            isResizingInner ? "bg-primary/40" : "hover:bg-primary/20"
          )}
        >
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-0.5 h-8 rounded-full bg-white/10 group-hover:bg-primary/40 transition-colors" />
        </div>
      </div>

      <div className="w-2 flex-shrink-0" />

      {/* COLUMN 3: Main Content Displayer (Canvas / Board / List) */}
      <div className="flex-1 min-w-0 h-full bg-foreground/[0.01] rounded-xl border border-white/[0.02] overflow-hidden">
        {mainContent}
      </div>

      {/* COLUMN 4: Resizable Context Pane (Feature Document / Execution) */}
      {isContextOpen && (
        <>
          <div className="w-2 flex-shrink-0" />
          <div 
            style={{ width: contextPaneWidth }} 
            className="flex-shrink-0 relative flex flex-col h-full bg-foreground/[0.03] rounded-xl border border-white/[0.05] shadow-2xl"
          >
            {/* RESIZE HANDLE */}
            <div
              onMouseDown={startResizingContext}
              className={cn(
                "absolute top-0 -left-1 w-2 h-full cursor-col-resize z-50 transition-all duration-300 group",
                isResizingContext ? "bg-primary/40" : "hover:bg-primary/20"
              )}
            >
              <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-0.5 h-8 rounded-full bg-white/10 group-hover:bg-primary/40 transition-colors" />
            </div>
            
            <div className="flex-1 overflow-hidden">
              {contextPane}
            </div>
          </div>
        </>
      )}
    </div>
  );
}

import { Suspense } from "react";
import { Outlet } from "@tanstack/react-router";

export function ContentDisplayer() {
  return (
    <div className="flex-1 overflow-auto bg-muted/20 border border-border/50 rounded-md shadow-inner h-full">
      <div className="container mx-auto p-6 max-w-full">
        <Suspense
          fallback={
            <div className="flex px-4 py-8 items-center justify-center text-sm font-mono tracking-widest uppercase text-muted-foreground/60 w-full animate-pulse border-2 border-dashed border-border/50 rounded-md">
              Synchronizing Nodes...
            </div>
          }
        >
          <Outlet />
        </Suspense>
      </div>
    </div>
  );
}

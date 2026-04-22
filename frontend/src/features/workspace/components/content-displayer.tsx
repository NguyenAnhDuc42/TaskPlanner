import { Suspense } from "react";
import { Outlet } from "@tanstack/react-router";

export function ContentDisplayer() {
  return (
    <div className="flex-1 bg-transparent h-full flex flex-col relative">
      <Suspense
        fallback={
          <div className="flex m-6 p-8 items-center justify-center text-sm font-mono tracking-widest uppercase text-muted-foreground/60 w-full animate-pulse rounded-md">
            Synchronizing Nodes...
          </div>
        }
      >
        <Outlet />
      </Suspense>
    </div>
  );
}

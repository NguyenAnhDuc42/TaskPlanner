"use client";

import { useSidebarContext } from "./sidebar-provider";
import { ContentSidebar } from "./content-sidebar";
import { cn } from "@/lib/utils";

export function ContentDisplayer() {
  const { activeContent, isInnerSidebarOpen } = useSidebarContext();

  return (
    <div className="flex-1 flex overflow-hidden bg-background border rounded-xl shadow-lg">
      {/* Inner Sidebar - Collapsible */}
      <div
        className={cn(
          "transition-all duration-300 ease-in-out overflow-hidden",
          isInnerSidebarOpen ? "w-64" : "w-0"
        )}
      >
        <div className="w-64 h-full">
          <ContentSidebar />
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 overflow-auto bg-muted/30">
        <div className="container mx-auto p-6">
          <div className="space-y-4">
            <div>
              <h1 className="text-3xl font-bold capitalize">{activeContent}</h1>
              <p className="text-muted-foreground">
                Content for {activeContent} page goes here
              </p>
            </div>

            {/* Placeholder content - replace with actual content components */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {[1, 2, 3, 4, 5, 6].map((i) => (
                <div
                  key={i}
                  className="rounded-lg border bg-card p-6 shadow-sm"
                >
                  <h3 className="font-semibold">Card {i}</h3>
                  <p className="text-sm text-muted-foreground mt-2">
                    This is placeholder content for the {activeContent} section.
                  </p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
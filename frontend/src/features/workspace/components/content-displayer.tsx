import { Suspense } from "react";
import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { ChevronLeft } from "lucide-react";
import { Separator } from "@/components/ui/separator";

import { SidebarRegistry } from "./sidebar-registry";

import { Outlet, useLocation } from "@tanstack/react-router";

export function ContentDisplayer() {
  const location = useLocation();
  const { isInnerSidebarOpen, toggleInnerSidebar } = useSidebarContext();

  // Determine active feature from path segments
  // Path format: /workspace/$id/$feature/...
  // Segments: ["", "workspace", "123", "members"]
  const segments = location.pathname.split("/");
  const activeContent = (segments[3] || "dashboard") as ContentPage;

  const displayTitle = ["tasks", "spaces", "folders", "lists"].includes(
    activeContent,
  )
    ? "Tasks"
    : activeContent;

  return (
    <div className="flex-1 flex overflow-hidden bg-background border rounded-xl shadow-lg">
      {/* SIDEBAR FRAME: Directly managed here in the displayer */}
      <div
        className={cn(
          "transition-all duration-300 ease-in-out overflow-hidden border-r flex flex-col bg-background",
          isInnerSidebarOpen ? "w-64" : "w-0",
        )}
      >
        <div className="w-64 h-full flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between p-4 flex-shrink-0">
            <h2 className="font-semibold capitalize text-lg">{displayTitle}</h2>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={toggleInnerSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </div>
          <Separator />

          {/* Scrollable List from Registry */}
          <ScrollArea className="flex-1 px-3 py-4">
            <SidebarRegistry page={activeContent} />
          </ScrollArea>
        </div>
      </div>

      {/* CONTENT FRAME */}
      <div className="flex-1 overflow-auto bg-muted/30">
        <div className="container mx-auto p-6 max-w-5xl">
          <Suspense
            fallback={
              <div className="p-8 text-center text-muted-foreground">
                Loading...
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </div>
      </div>
    </div>
  );
}

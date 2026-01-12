import { Suspense } from "react";
import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage, getNavigationItems } from "../type";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { ChevronLeft } from "lucide-react";
import { Separator } from "@/components/ui/separator";

import { Outlet, useLocation } from "@tanstack/react-router";

export function ContentDisplayer() {
  const location = useLocation();
  const { isInnerSidebarOpen, toggleInnerSidebar, sidebarContent } =
    useSidebarContext();

  // Determine active feature from path segments
  const segments = location.pathname.split("/");
  const activeContent = (segments[segments.length - 1] ||
    "dashboard") as ContentPage;

  const navItems = getNavigationItems(activeContent);

  return (
    <div className="flex-1 flex overflow-hidden bg-background border rounded-xl shadow-lg">
      {/* SIDEBAR FRAME: Directly managed here in the displayer */}
      <div
        className={cn(
          "transition-all duration-300 ease-in-out overflow-hidden border-r flex flex-col bg-background",
          isInnerSidebarOpen ? "w-64" : "w-0"
        )}
      >
        <div className="w-64 h-full flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between p-4 flex-shrink-0">
            <h2 className="font-semibold capitalize text-lg">
              {activeContent}
            </h2>
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

          {/* Scrollable list of links */}
          <ScrollArea className="flex-1 px-3 py-4">
            {sidebarContent ? (
              sidebarContent
            ) : (
              <div className="space-y-1">
                {navItems.map((item) => {
                  const Icon = item.icon;
                  return (
                    <Button
                      key={item.id}
                      variant="ghost"
                      className="w-full justify-start gap-3 px-3 py-2 h-10 transition-colors hover:bg-accent/50"
                    >
                      <Icon className="h-4 w-4" />
                      <span className="text-sm font-medium">{item.label}</span>
                    </Button>
                  );
                })}
              </div>
            )}
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

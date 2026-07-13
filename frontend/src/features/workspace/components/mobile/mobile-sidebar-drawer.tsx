import { useEffect } from "react";
import { useLocation } from "@tanstack/react-router";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { HierarchySidebar } from "../../contents/hierarchy/hierarchy-sidebar";

interface MobileSidebarDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

// Wraps the same HierarchySidebar the desktop inner sidebar renders — no duplicate navigation
// tree, just a different container. Closes itself on route change (selecting a space/task).
export function MobileSidebarDrawer({ open, onOpenChange }: MobileSidebarDrawerProps) {
  const location = useLocation();

  useEffect(() => {
    onOpenChange(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="p-0 w-4/5">
        <SheetTitle className="sr-only">Navigation</SheetTitle>
        <div className="h-full w-full pt-10">
          <HierarchySidebar />
        </div>
      </SheetContent>
    </Sheet>
  );
}

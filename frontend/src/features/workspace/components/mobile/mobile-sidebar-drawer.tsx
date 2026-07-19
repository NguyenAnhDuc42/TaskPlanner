import { useEffect } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { ListTodo } from "lucide-react";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { useWorkspace } from "../../context/workspace-context";
import { FavoritesProjectsNav } from "../favorites-projects-nav";

interface MobileSidebarDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

// Mirrors desktop's AppSidebar nav (My Tasks + Favorites + flat Projects list) rather than the
// old nested Space>Folder>Task tree — Inbox/Members/Settings are already reachable from
// MobileTabBar, so this only needs to cover what the tab bar doesn't. Closes itself on route change.
export function MobileSidebarDrawer({ open, onOpenChange }: MobileSidebarDrawerProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();

  useEffect(() => {
    onOpenChange(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="p-0 w-4/5">
        <SheetTitle className="sr-only">Navigation</SheetTitle>
        <div className="h-full w-full pt-10 flex flex-col overflow-hidden">
          <nav className="px-2 pb-2 shrink-0">
            <button
              type="button"
              onClick={() => navigate({ to: "/workspaces/$workspaceId/my-tasks", params: { workspaceId } })}
              className="flex items-center gap-2 h-8 px-2 rounded-md w-full text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer"
            >
              <ListTodo className="h-3.5 w-3.5" />
              <span className="text-[11px] font-semibold">My Tasks</span>
            </button>
          </nav>
          <FavoritesProjectsNav />
        </div>
      </SheetContent>
    </Sheet>
  );
}

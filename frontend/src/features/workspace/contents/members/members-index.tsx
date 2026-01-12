"use client";

import { useEffect } from "react";
import { useSidebarContext } from "../../components/sidebar-provider";
import { Button } from "@/components/ui/button";
import { UserPlus } from "lucide-react";

export default function MembersIndex() {
  const { workspaceId, setSidebarContent } = useSidebarContext();

  // INJECT DYNAMIC SIDEBAR CONTENT
  useEffect(() => {
    setSidebarContent(
      <div className="space-y-4">
        <div className="px-1 py-2">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            Workspace Team
          </h3>
          <div className="space-y-1">
            <Button variant="ghost" className="w-full justify-start gap-2 h-9">
              <div className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold">
                JD
              </div>
              <span>John Doe</span>
            </Button>
            <Button variant="ghost" className="w-full justify-start gap-2 h-9">
              <div className="h-6 w-6 rounded-full bg-blue-500/20 flex items-center justify-center text-[10px] font-bold">
                AS
              </div>
              <span>Alice Smith</span>
            </Button>
          </div>
        </div>

        <Button variant="outline" className="w-full gap-2 border-dashed">
          <UserPlus className="h-4 w-4" />
          <span>Invite Member</span>
        </Button>
      </div>
    );

    // CLEANUP ON UNMOUNT (important to reset the sidebar)
    return () => setSidebarContent(null);
  }, [setSidebarContent]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Members</h1>
          <p className="text-muted-foreground">
            Manage members for workspace:{" "}
            <span className="font-mono text-primary">{workspaceId}</span>
          </p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {/* Mocking members list */}
        {[1, 2, 3].map((i) => (
          <div key={i} className="rounded-xl border bg-card p-6 shadow-sm">
            <div className="flex items-center gap-4 mb-4">
              <div className="h-10 w-10 rounded-full bg-muted" />
              <div>
                <p className="font-medium">Team Member {i}</p>
                <p className="text-sm text-muted-foreground text-xs">
                  member@example.com
                </p>
              </div>
            </div>
            <Button variant="secondary" size="sm" className="w-full">
              Edit Perms
            </Button>
          </div>
        ))}
      </div>
    </div>
  );
}

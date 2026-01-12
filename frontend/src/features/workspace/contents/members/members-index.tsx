"use client";

import { useSidebarContext } from "../../components/sidebar-provider";
import { Button } from "@/components/ui/button";

export default function MembersIndex() {
  const { workspaceId } = useSidebarContext();

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

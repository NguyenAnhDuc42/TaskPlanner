"use client";

import { useSidebarContext } from "../../components/sidebar-provider";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";

export default function SettingsIndex() {
  const { workspaceId } = useSidebarContext();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">
          Manage your workspace preferences and configurations for:{" "}
          <span className="font-mono text-primary">{workspaceId}</span>
        </p>
      </div>
      <Separator />

      <div className="grid gap-8">
        <section className="space-y-4">
          <h2 className="text-xl font-semibold">Workspace Profile</h2>
          <div className="grid gap-4 max-w-2xl border rounded-xl p-6 bg-card shadow-sm">
            <div className="space-y-2">
              <label className="text-sm font-medium">Workspace Name</label>
              <input
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                placeholder="Enter workspace name"
                defaultValue={`Workspace ${workspaceId}`}
              />
            </div>
            <Button className="w-fit">Save Changes</Button>
          </div>
        </section>

        <section className="space-y-4 text-destructive">
          <h2 className="text-xl font-semibold">Danger Zone</h2>
          <div className="border border-destructive/20 rounded-xl p-6 bg-destructive/5 space-y-4 max-w-2xl">
            <p className="text-sm">
              Deleting this workspace will remove all associated data, including
              tasks, members, and settings. This action is irreversible.
            </p>
            <Button variant="destructive">Delete Workspace</Button>
          </div>
        </section>
      </div>
    </div>
  );
}

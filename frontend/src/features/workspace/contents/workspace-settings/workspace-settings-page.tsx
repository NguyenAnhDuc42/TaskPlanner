import { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "@tanstack/react-router";
import { UniversalPicker } from "@/components/universal-picker";
import { DeleteConfirmationDialog } from "../hierarchy/hierarchy-components/context-menus/shared";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import { toast } from "sonner";

export const WorkspaceSettingsPage = observer(function WorkspaceSettingsPage() {
  const { workspaceId, workspace } = useWorkspace();
  const { isOwner, canEditWorkspace } = useWorkspaceRole();
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const navigate = useNavigate();
  const workspaceMutations = useMemo(() => new WorkspaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  if (!workspace) return null;

  const updateField = (patches: Parameters<WorkspaceMutations["update"]>[1]) => {
    workspaceMutations.update(workspaceId, patches).catch((err) => {
      console.error("Failed to update workspace", err);
      toast.error("Failed to update workspace");
    });
  };

  const handleDelete = async () => {
    try {
      await workspaceMutations.delete(workspaceId);
      localStorage.removeItem("lastWorkspaceId");
      navigate({ to: "/" });
    } catch (err) {
      console.error("Failed to delete workspace", err);
      toast.error("Failed to delete workspace");
    }
  };

  return (
    <div className="h-full w-full overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
      <div className="max-w-xl mx-auto px-8 py-10 flex flex-col gap-8">
        <h1 className="text-lg font-black text-foreground/90 tracking-tight">Workspace Settings</h1>

        {/* Profile */}
        <section className="flex flex-col gap-4">
          <div className="flex items-center gap-3">
            <UniversalPicker
              icon={workspace.icon || "LayoutGrid"}
              color={workspace.color || "#3b82f6"}
              onSelect={(icon, color) => updateField({ icon, color })}
              size="lg"
            />
            <div className="min-w-0 flex-1">
              <input
                key={workspaceId}
                className="text-lg font-bold text-foreground/90 tracking-tight leading-none bg-transparent border-none outline-none w-full hover:bg-muted/20 focus:bg-muted/30 px-1 rounded transition-colors disabled:cursor-default disabled:hover:bg-transparent"
                defaultValue={workspace.name}
                disabled={!canEditWorkspace}
                onBlur={(e) => {
                  if (canEditWorkspace && e.target.value.trim() && e.target.value !== workspace.name)
                    updateField({ name: e.target.value.trim() });
                }}
                onKeyDown={(e) => { if (e.key === "Enter") e.currentTarget.blur(); }}
              />
            </div>
          </div>
        </section>

        {/* Danger Zone */}
        {isOwner && (
          <section className="flex flex-col gap-2 pt-6 border-t border-border/20">
            <span className="text-[10px] font-bold text-destructive/70 uppercase tracking-wide">Danger Zone</span>
            <div className="flex items-center justify-between border border-destructive/20 rounded-md px-3 py-2.5">
              <div className="flex flex-col">
                <span className="text-xs font-semibold text-foreground/80">Delete workspace</span>
                <span className="text-[10px] text-muted-foreground/60">Permanently deletes all spaces, tasks, and members.</span>
              </div>
              <button
                type="button"
                onClick={() => setIsDeleteOpen(true)}
                className="text-[10px] font-bold text-destructive hover:bg-destructive/10 px-2.5 py-1.5 rounded-md transition-colors cursor-pointer shrink-0"
              >
                Delete
              </button>
            </div>
          </section>
        )}
      </div>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Workspace"
        description={`Are you sure you want to delete "${workspace.name}"? This will delete all spaces, tasks, and members and cannot be undone.`}
        onConfirm={handleDelete}
      />
    </div>
  );
});

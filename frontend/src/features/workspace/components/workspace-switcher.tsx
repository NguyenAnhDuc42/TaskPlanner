import { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { ChevronDown, Check, Plus, Pin, LogIn } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "../context/workspace-context";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate } from "@tanstack/react-router";
import { CreateWorkspaceForm } from "@/features/main/home-screen/components/create-workspace-form";
import { JoinWorkspaceDialog } from "@/features/main/home-screen/components/join-workspace-dialog";
import { RoleBadge } from "@/components/role-badge";
import type { Role } from "@/types/role";
import type { FormData } from "@/features/main/home-screen/components/create-workspace-form";
import { signalRService } from "@/lib/signalr-service";
import { toast } from "sonner";
import React from "react";

export const WorkspaceSwitcher = observer(function WorkspaceSwitcher() {
  const [open, setOpen] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [showJoin, setShowJoin] = useState(false);
  const [isJoining, setIsJoining] = React.useState(false);

  const { workspaceId } = useWorkspace();
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const workspaceMutations = React.useMemo(() => new WorkspaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  useEffect(() => {
    workspaceMutations.fetchList({ direction: "Ascending" }).catch((err) => console.error("Failed to fetch workspaces", err));
  }, [workspaceMutations]);

  useEffect(() => {
    const onJoined = () => {
      workspaceMutations.fetchList({ direction: "Ascending" }).catch((err) => console.error("Failed to refresh workspaces", err));
    };
    signalRService.on("WorkspaceJoined", onJoined);
    return () => signalRService.off("WorkspaceJoined", onJoined);
  }, [workspaceMutations]);

  const workspaces = rootStore.workspaceStore.all;
  const navigate = useNavigate();
  const [isCreating, setIsCreating] = React.useState(false);

  const setPin = (payload: { workspaceId: string; isPinned: boolean }) => {
    workspaceMutations.pin(payload.workspaceId, payload.isPinned).catch((err) => {
      console.error("Failed to update pin", err);
      toast.error("Failed to update pin");
    });
  };

  const handleCreateWorkspace = async (data: FormData) => {
    setIsCreating(true);
    try {
      await workspaceMutations.create({ ...data, theme: "Dark" });
      setShowCreate(false);
    } finally {
      setIsCreating(false);
    }
  };

  const activeWorkspace = workspaces.find((ws: WorkspaceRecord) => ws.id === workspaceId);

  const handleEnter = (ws: WorkspaceRecord) => {
    localStorage.setItem("lastWorkspaceId", ws.id);
    navigate({ to: `/workspaces/${ws.id}` });
    setOpen(false);
  };

  const handleJoin = async (code: string) => {
    setIsJoining(true);
    try {
      const result = await workspaceMutations.joinByCode(code);
      if (result.membershipStatus === "Pending") {
        toast.info("Join request sent. Waiting for approval.");
      } else {
        toast.success("Joined workspace successfully");
        await workspaceMutations.fetchList({ direction: "Ascending" });
      }
    } catch (err: unknown) {
      const error = err as { message?: string; data?: { Description?: string } };
      toast.error(error.data?.Description || error.message || "Failed to join workspace");
    } finally {
      setIsJoining(false);
      setShowJoin(false);
    }
  };

  if (!activeWorkspace) return null;

  return (
    <>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            className="flex items-center gap-1.5 px-1.5 py-1 rounded-md hover:bg-muted/50 transition-colors cursor-pointer group outline-none"
          >
            <div
              className="h-4 w-4 rounded flex items-center justify-center shrink-0"
              style={{ backgroundColor: activeWorkspace.color || "#6366f1" }}
            >
              <DynamicIcon name={activeWorkspace.icon || "LayoutGrid"} size={10} className="text-white" />
            </div>
            <span className="text-[11px] font-bold text-foreground/80 group-hover:text-foreground transition-colors max-w-[120px] truncate">
              {activeWorkspace.name}
            </span>
            <ChevronDown className={cn("h-3 w-3 text-muted-foreground/50 transition-transform shrink-0", open && "rotate-180")} />
          </button>
        </PopoverTrigger>

        <PopoverContent
          align="start"
          className="w-56 p-0 gap-0 rounded-md border border-border shadow-md bg-background text-popover-foreground overflow-hidden"
        >
          <div className="px-2 py-1.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground opacity-60">
            Workspaces
          </div>
          <div className="h-px w-full bg-border" />

          <div className="flex flex-col gap-px">
            {workspaces.map((ws: WorkspaceRecord) => {
              const isActive = ws.id === workspaceId;
              const canPin = !!ws.role;

              return (
                  <div
                    key={ws.id}
                    className={cn(
                      "flex items-center gap-1 rounded-none transition-colors",
                      isActive ? "bg-muted" : "hover:bg-muted/60"
                    )}
                  >
                  {/* Pin — separate from nav row */}
                  {canPin && (
                    <button
                      type="button"
                      className="shrink-0 h-7 w-6 flex items-center justify-center rounded-md hover:bg-muted-foreground/10 transition-colors ml-0.5"
                      onClick={() => setPin({ workspaceId: ws.id, isPinned: !ws.isPinned })}
                    >
                      <Pin className={cn("h-2.5 w-2.5", ws.isPinned ? "fill-primary text-primary" : "text-muted-foreground/30")} />
                    </button>
                  )}

                  {/* Nav row */}
                  <button
                    type="button"
                    disabled={isActive}
                    onClick={() => !isActive && handleEnter(ws)}
                    className={cn(
                      "flex-1 flex items-center gap-2 px-2 py-1.5 rounded-none text-left transition-colors min-w-0",
                      isActive ? "text-foreground cursor-default" : "text-muted-foreground hover:text-foreground cursor-pointer"
                    )}
                  >
                    <div
                      className="h-5 w-5 rounded flex items-center justify-center shrink-0"
                      style={{ backgroundColor: ws.color || "#6366f1" }}
                    >
                      <DynamicIcon name={ws.icon || "LayoutGrid"} size={10} className="text-white" />
                    </div>
                    <span className="flex-1 text-[11px] font-semibold truncate">{ws.name}</span>
                    <div className="flex items-center gap-1 shrink-0">
                      {ws.role && <RoleBadge role={ws.role as Role} />}
                      {isActive && <Check className="h-3 w-3 text-primary" />}
                    </div>
                  </button>
                </div>
              );
            })}
          </div>

          <div className="h-px w-full bg-border" />

          <div className="flex flex-col gap-0">
            <button
              type="button"
              className="flex items-center gap-2 px-2 py-1.5 rounded-none text-[11px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors"
              onClick={() => { setShowCreate(true); setOpen(false); }}
            >
              <Plus className="h-3.5 w-3.5" />
              New Workspace
            </button>
            <button
              type="button"
              className="flex items-center gap-2 px-2 py-1.5 rounded-none text-[11px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors"
              onClick={() => { setShowJoin(true); setOpen(false); }}
            >
              <LogIn className="h-3.5 w-3.5" />
              Join with Code
            </button>
          </div>
        </PopoverContent>
      </Popover>

      <CreateWorkspaceForm
        open={showCreate}
        onOpenChange={setShowCreate}
        isLoading={isCreating}
        onSubmit={handleCreateWorkspace}
      />

      <JoinWorkspaceDialog
        open={showJoin}
        onOpenChange={setShowJoin}
        isLoading={isJoining}
        onJoin={handleJoin}
      />
    </>
  );
});

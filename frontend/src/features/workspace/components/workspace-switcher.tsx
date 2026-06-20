import { useState } from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { ChevronDown, Check, Plus, Pin, LogIn } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "../context/workspace-context";
import { useSelector } from "react-redux";
import { workspaceSelectors } from "@/store/entityStore";
import type { WorkspaceSnippetRecord } from "@/types/workspace";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate } from "@tanstack/react-router";
import { CreateWorkspaceForm } from "@/features/main/home-screen/components/create-workspace-form";
import { useSetWorkspacePin, useGetWorkspacesQuery, useCreateWorkspace, useJoinWorkspaceByCode } from "@/features/main/home-screen/api";
import { JoinWorkspaceDialog } from "@/features/main/home-screen/components/join-workspace-dialog";
import { RoleBadge } from "@/components/role-badge";
import type { Role } from "@/types/role";
import type { FormData } from "@/features/main/home-screen/components/create-workspace-form";
import React from "react";

export function WorkspaceSwitcher() {
  const [open, setOpen] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [showJoin, setShowJoin] = useState(false);
  const [isJoining, setIsJoining] = React.useState(false);

  const { workspaceId } = useWorkspace();
  useGetWorkspacesQuery({ direction: "Ascending" });

  const workspaces = useSelector(workspaceSelectors.selectAll);
  const navigate = useNavigate();
  const { mutate: setPin } = useSetWorkspacePin();
  const { mutate: createWorkspace } = useCreateWorkspace();
  const { mutate: joinByCode } = useJoinWorkspaceByCode();
  const [isCreating, setIsCreating] = React.useState(false);

  const handleCreateWorkspace = async (data: FormData) => {
    setIsCreating(true);
    try {
      await createWorkspace({ ...data, theme: "Dark" });
      setShowCreate(false);
    } finally {
      setIsCreating(false);
    }
  };

  const activeWorkspace = workspaces.find((ws: WorkspaceSnippetRecord) => ws.id === workspaceId);

  const handleEnter = (ws: WorkspaceSnippetRecord) => {
    localStorage.setItem("lastWorkspaceId", ws.id);
    navigate({ to: `/workspaces/${ws.id}` });
    setOpen(false);
  };

  const handleJoin = async (code: string) => {
    setIsJoining(true);
    try {
      await joinByCode(code);
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
          className="w-56 p-1.5 bg-background border border-border/40 shadow-xl rounded-lg"
        >
          <p className="px-1.5 pb-1 text-[9px] font-black uppercase tracking-widest text-muted-foreground/40">
            Workspaces
          </p>

          <div className="flex flex-col gap-px">
            {workspaces.map((ws: WorkspaceSnippetRecord) => {
              const isActive = ws.id === workspaceId;
              const canPin = !!ws.role;

              return (
                <div
                  key={ws.id}
                  className={cn(
                    "flex items-center gap-1 rounded-md transition-colors",
                    isActive ? "bg-primary/8" : "hover:bg-muted/60"
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
                      "flex-1 flex items-center gap-2 px-1.5 py-1.5 rounded-md text-left transition-colors min-w-0",
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

          <div className="h-px bg-border/30 my-1" />

          <div className="flex flex-col gap-px">
            <button
              type="button"
              className="flex items-center gap-2 px-1.5 py-1.5 rounded-md text-[11px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors"
              onClick={() => { setShowCreate(true); setOpen(false); }}
            >
              <Plus className="h-3.5 w-3.5" />
              New Workspace
            </button>
            <button
              type="button"
              className="flex items-center gap-2 px-1.5 py-1.5 rounded-md text-[11px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors"
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
}

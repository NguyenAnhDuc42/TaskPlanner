import { CreateWorkspaceForm } from "./components/create-workspace-form";
import { JoinWorkspaceDialog } from "./components/join-workspace-dialog";
import { useWorkspaceHome, useJoinWorkspaceByCode } from "./api";
import { DynamicIcon } from "@/components/dynamic-icon";
import { RoleBadge } from "@/components/role-badge";
import { Button } from "@/components/ui/button";
import { useNavigate } from "@tanstack/react-router";
import { Pin, Plus, LogIn, ChevronRight, User, LogOut } from "lucide-react";
import { cn } from "@/lib/utils";
import React, { useState } from "react";
import type { Role } from "@/types/role";
import type { WorkspaceSnippetRecord } from "@/types/workspace";
import { useLogout, useGetMeQuery } from "@/features/auth/auth-api";
import { ProfileModal } from "@/features/auth/profile/components/profile-modal";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

function UserMenu({ onOpenProfile }: { onOpenProfile: () => void }) {
  const { data: user } = useGetMeQuery();
  const { mutate: logout } = useLogout();

  const initials = user?.name
    ? user.name.split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase()
    : "?";

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="h-7 w-7 rounded-lg bg-linear-to-tr from-primary/20 to-primary/5 border border-primary/20 flex items-center justify-center cursor-pointer hover:border-primary/40 transition-colors shadow-sm outline-none"
        >
          <span className="text-[10px] font-black text-primary">{initials}</span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-52 bg-popover border border-border/30 shadow-xl rounded-lg p-1">
        <DropdownMenuLabel className="px-2 py-1.5">
          <p className="text-xs font-bold text-foreground/90 truncate">{user?.name ?? "User"}</p>
          <p className="text-[10px] text-muted-foreground/50 font-medium truncate">{user?.email ?? ""}</p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="bg-border/20 my-1" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-2 py-1.5 text-xs font-medium text-muted-foreground/70 hover:text-foreground cursor-pointer rounded-md"
          onClick={onOpenProfile}
        >
          <User className="h-3.5 w-3.5" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuSeparator className="bg-border/20 my-1" />
        <DropdownMenuItem
          className="flex items-center gap-2 px-2 py-1.5 text-xs font-medium text-destructive/80 hover:text-destructive hover:bg-destructive/10 cursor-pointer rounded-md"
          onClick={() => logout()}
        >
          <LogOut className="h-3.5 w-3.5" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export function WorkspaceHomeScreen() {
  const [profileOpen, setProfileOpen] = useState(false);

  const {
    workspaces,
    isWorkspacesLoading,
    isCreating,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isJoinModalOpen,
    setIsJoinModalOpen,
    handleCreateWorkspace,
    handlePinWorkspace,
  } = useWorkspaceHome();

  const [isJoining, setIsJoining] = React.useState(false);
  const { mutate: joinByCode } = useJoinWorkspaceByCode();
  const navigate = useNavigate();

  const handleJoin = async (code: string) => {
    setIsJoining(true);
    try {
      await joinByCode(code);
    } finally {
      setIsJoining(false);
    }
  };

  const handleEnter = (ws: WorkspaceSnippetRecord) => {
    localStorage.setItem("lastWorkspaceId", ws.id);
    navigate({ to: `/workspaces/${ws.id}` });
  };

  const canPin = (ws: WorkspaceSnippetRecord) => !!ws.role;

  return (
    <div className="flex flex-col items-center justify-center h-screen bg-background overflow-hidden relative">
      <div className="absolute h-[500px] w-[500px] bg-primary/5 rounded-full blur-[120px] -z-10 top-1/4 left-1/3" />

      {/* Top-right user menu */}
      <div className="absolute top-3 right-3">
        <UserMenu onOpenProfile={() => setProfileOpen(true)} />
      </div>

      <div className="w-full max-w-sm flex flex-col gap-3 px-4">
        {/* Header */}
        <div className="mb-1">
          <h1 className="text-lg font-black tracking-tight text-foreground">Workspaces</h1>
          <p className="text-[11px] text-muted-foreground/60">Select a workspace to continue</p>
        </div>

        {/* List */}
        <div className="flex flex-col gap-0.5 max-h-[55vh] overflow-y-auto">
          {isWorkspacesLoading ? (
            <div className="py-8 text-center text-xs text-muted-foreground/40">Loading...</div>
          ) : workspaces.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted-foreground/40">No workspaces yet.</div>
          ) : (
            workspaces.map((ws) => (
              <div
                key={ws.id}
                role="button"
                tabIndex={0}
                className="flex items-center gap-2.5 px-2 py-2 rounded-md border border-transparent hover:bg-muted/40 hover:border-border/30 cursor-pointer transition-colors group outline-none focus-visible:ring-1 focus-visible:ring-primary"
                onClick={() => handleEnter(ws)}
                onKeyDown={(e) => (e.key === "Enter" || e.key === " ") && handleEnter(ws)}
              >
                {/* Pin */}
                {canPin(ws) && (
                  <button
                    type="button"
                    className="shrink-0 h-5 w-5 flex items-center justify-center rounded hover:bg-muted-foreground/10 transition-colors"
                    onClick={(e) => {
                      e.stopPropagation();
                      handlePinWorkspace?.(ws.id, !ws.isPinned);
                    }}
                  >
                    <Pin className={cn("h-2.5 w-2.5 transition-colors", ws.isPinned ? "fill-primary text-primary" : "text-muted-foreground/30 hover:text-muted-foreground")} />
                  </button>
                )}

                {/* Icon */}
                <div
                  className="h-7 w-7 rounded-md flex items-center justify-center shrink-0 border"
                  style={{ backgroundColor: `${ws.color || "#6366f1"}18`, borderColor: `${ws.color || "#6366f1"}30` }}
                >
                  <DynamicIcon name={ws.icon || "LayoutGrid"} color={ws.color || "#6366f1"} size={14} />
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <p className="text-[12px] font-semibold text-foreground/90 truncate group-hover:text-primary transition-colors">
                    {ws.name}
                  </p>
                  {ws.memberCount != null && (
                    <p className="text-[10px] text-muted-foreground/50">{ws.memberCount} members</p>
                  )}
                </div>

                {/* Right */}
                <div className="flex items-center gap-1.5 shrink-0">
                  {ws.role && <RoleBadge role={ws.role as Role} />}
                  <ChevronRight className="h-3 w-3 text-muted-foreground/30 group-hover:text-muted-foreground transition-colors" />
                </div>
              </div>
            ))
          )}
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 pt-1 border-t border-border/30">
          <Button
            size="sm"
            className="flex-1 h-8 text-[11px] font-semibold gap-1.5"
            onClick={() => setIsCreateModalOpen(true)}
          >
            <Plus className="h-3.5 w-3.5" />
            New Workspace
          </Button>
          <Button
            size="sm"
            variant="outline"
            className="h-8 text-[11px] font-semibold gap-1.5 border-border/40"
            onClick={() => setIsJoinModalOpen(true)}
          >
            <LogIn className="h-3.5 w-3.5" />
            Join
          </Button>
        </div>
      </div>

      <CreateWorkspaceForm
        open={isCreateModalOpen}
        onOpenChange={setIsCreateModalOpen}
        isLoading={isCreating}
        onSubmit={handleCreateWorkspace}
      />

      <JoinWorkspaceDialog
        open={isJoinModalOpen}
        onOpenChange={setIsJoinModalOpen}
        isLoading={isJoining}
        onJoin={handleJoin}
      />

      <ProfileModal open={profileOpen} onOpenChange={setProfileOpen} />
    </div>
  );
}

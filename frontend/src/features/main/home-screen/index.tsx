import { CreateWorkspaceForm } from "./components/create-workspace-form";
import { JoinWorkspaceDialog } from "./components/join-workspace-dialog";
import { useWorkspaceHome, useJoinWorkspaceByCode } from "./api";
import { DynamicIcon } from "@/components/dynamic-icon";
import { RoleBadge } from "@/components/role-badge";
import { Button } from "@/components/ui/button";
import { useNavigate } from "@tanstack/react-router";
import { ChevronRight, Pin } from "lucide-react";
import { cn } from "@/lib/utils";

export function WorkspaceHomeScreen() {
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

  const { mutate: joinByCode, isPending: isJoining } = useJoinWorkspaceByCode();
  const navigate = useNavigate();

  return (
    <div className="flex flex-col items-center justify-center h-screen bg-background overflow-hidden relative">
      {/* Subtle Background Glows */}
      <div className="absolute inset-0 bg-grid-white/[0.02] -z-10" />
      <div className="absolute h-96 w-96 bg-primary/10 rounded-full blur-3xl -z-10 top-1/4 left-1/4 animate-pulse" />
      <div className="absolute h-96 w-96 bg-secondary/10 rounded-full blur-3xl -z-10 bottom-1/4 right-1/4 animate-pulse" />

      <div className="w-full max-w-md flex flex-col gap-6 p-6">
        {/* Header */}
        <div className="text-center space-y-1">
          <h1 className="text-2xl font-black tracking-tight text-foreground">Your Workspaces</h1>
          <p className="text-muted-foreground text-xs">Choose a workspace to continue working</p>
        </div>

        {/* Workspace List Container */}
        <div className="bg-muted/20 backdrop-blur-md border border-border/40 rounded-md p-2 space-y-1 max-h-[50vh] overflow-y-auto no-scrollbar shadow-2xl">
          {isWorkspacesLoading ? (
            <div className="p-4 text-center text-muted-foreground text-sm">Loading...</div>
          ) : workspaces.length === 0 ? (
            <div className="p-4 text-center text-muted-foreground text-sm">No workspaces found.</div>
          ) : (
            workspaces.map((workspace) => (
              <div
                key={workspace.id}
                className="flex items-center justify-between p-3 bg-background/40 hover:bg-background/80 border border-transparent hover:border-border/40 rounded-md cursor-pointer transition-all duration-200 group"
                onClick={() => {
                  localStorage.setItem("lastWorkspaceId", workspace.id);
                  navigate({ to: `/workspaces/${workspace.id}` });
                }}
              >
                <div className="flex items-center gap-3">
                  {/* Pin Button on the far left */}
                  <Button
                    size="icon"
                    variant="ghost"
                    className="h-6 w-6 text-muted-foreground hover:text-foreground rounded-md shrink-0"
                    onClick={(e) => {
                      e.stopPropagation();
                      handlePinWorkspace?.(workspace.id, !workspace.isPinned);
                    }}
                  >
                    <Pin className={cn("h-3 w-3", workspace.isPinned && "fill-primary text-primary")} />
                  </Button>

                  {/* Icon */}
                  <div 
                    className="h-9 w-9 rounded-md flex items-center justify-center text-sm font-bold border border-border/10 shrink-0"
                    style={{
                      backgroundColor: `${workspace.color}15`,
                      borderColor: `${workspace.color}40`,
                    }}
                  >
                    <DynamicIcon name={workspace.icon} color={workspace.color} size={18} />
                  </div>
                  
                  {/* Info */}
                  <div>
                    <h3 className="font-bold text-sm text-foreground group-hover:text-primary transition-colors">
                      {workspace.name}
                    </h3>
                    <p className="text-[10px] text-muted-foreground font-mono uppercase">
                      {workspace.variant} • {workspace.memberCount} members
                    </p>
                  </div>
                </div>

                {/* Right side affordance */}
                <div className="flex items-center gap-2">
                  <RoleBadge role={workspace.role} />
                  <ChevronRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground group-hover:translate-x-0.5 transition-all" />
                </div>
              </div>
            ))
          )}
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between gap-4">
          <Button
            variant="outline"
            className="rounded-md border-border/40 hover:bg-muted/50 flex-1 h-10 text-xs font-bold"
            onClick={() => setIsCreateModalOpen(true)}
          >
            Create Workspace
          </Button>
          <Button
            variant="ghost"
            className="text-muted-foreground rounded-md hover:text-foreground text-xs font-bold h-10"
            onClick={() => setIsJoinModalOpen(true)}
          >
            Join with Code
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
        onJoin={(code) => joinByCode(code)}
      />
    </div>
  );
}

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import React from "react";
import type { WorkspaceSummary } from "../type";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ChevronDown, PencilLine, Pin, Settings, Users } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Link } from "@tanstack/react-router";
import { Badge } from "@/components/ui/badge";
import type { Role } from "@/types/role";

type Props = {
  workspaceSummary: WorkspaceSummary;
  onOpen?: (id: string) => void;
  onPin?: (id: string, isPinned: boolean) => void;
};

function RoleBadge({ role }: { role: Role }) {
  if (role === "None") return null;

  let variant: "default" | "secondary" | "outline" | "destructive" = "outline";
  let className = "text-[10px] px-1.5 h-4 font-mono uppercase tracking-wider";

  switch (role) {
    case "Owner":
      variant = "default";
      className = cn(
        className,
        "bg-red-500/20 text-red-500 border-red-500/30 hover:bg-red-500/40 border",
      );
      break;
    case "Admin":
      variant = "outline";
      className = cn(
        className,
        "bg-blue-500/10 text-blue-500 border-blue-500/30 hover:bg-blue-500/20 border",
      );
      break;
    case "Member":
      variant = "secondary";
      className = cn(
        className,
        "bg-green-500/10 text-green-500 border-green-500/30 border",
      );
      break;
    case "Guest":
      variant = "secondary";
      className = cn(className, "opacity-60 border");
      break;
  }

  return (
    <Badge variant={variant} className={className}>
      {role}
    </Badge>
  );
}

export function WorkspaceItem({ workspaceSummary, onOpen, onPin }: Props) {
  const [open, setOpen] = React.useState(false);

  React.useEffect(() => {
    if (open) onOpen?.(workspaceSummary.id);
  }, [open, onOpen, workspaceSummary.id]);

  return (
    <Collapsible open={open} onOpenChange={setOpen}>
      <CollapsibleTrigger asChild>
        <div className="w-full border border-border bg-card hover:bg-card/80 transition-colors cursor-pointer group">
          <div className="flex items-center gap-4 p-4">
            {/* Icon with color accent */}
            <div
              className="h-10 w-10 flex items-center justify-center text-sm font-bold flex-shrink-0 border border-border/50"
              style={{
                backgroundColor: `${workspaceSummary.color}20`,
                borderColor: workspaceSummary.color,
              }}
            >
              <DynamicIcon
                name={workspaceSummary.icon}
                color={workspaceSummary.color}
                size={20}
              />
            </div>

            {/* Main content - name and role tag */}
            <div className="flex-1 min-w-0 flex items-center gap-2">
              <h3 className="font-mono font-bold text-sm text-foreground truncate tracking-tight">
                {workspaceSummary.name}
              </h3>
              <RoleBadge role={workspaceSummary.role} />
            </div>

            <Button
              size="sm"
              variant="ghost"
              className="h-6 w-6 p-0 flex-shrink-0 text-muted-foreground hover:text-primary transition-colors"
              disabled={!workspaceSummary.canPinWorkspace}
              title={
                workspaceSummary.canPinWorkspace
                  ? "Pin workspace"
                  : "You cannot pin this workspace"
              }
              onClick={(e) => {
                e.stopPropagation();
                onPin?.(workspaceSummary.id, !workspaceSummary.isPinned);
              }}
            >
              <Pin
                className={cn(
                  "h-4 w-4",
                  workspaceSummary.isPinned && "fill-primary text-primary",
                )}
              />
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="h-6 w-6 p-0 flex-shrink-0 text-muted-foreground hover:text-primary transition-colors"
              disabled={!workspaceSummary.canUpdateWorkspace}
              title={
                workspaceSummary.canUpdateWorkspace
                  ? "Workspace settings"
                  : "Only owner/admin can edit workspace settings"
              }
            >
              <PencilLine className={cn("h-4 w-4")} />
            </Button>

            {/* Chevron */}
            <ChevronDown
              className={cn(
                "h-4 w-4 text-muted-foreground transition-transform flex-shrink-0 group-hover:text-foreground",
                open && "rotate-180",
              )}
            />
          </div>
        </div>
      </CollapsibleTrigger>

      <CollapsibleContent>
        <div className="border border-t-0 border-border bg-card/50">
          <div className="px-4 py-3 border-b border-border/50 text-xs text-muted-foreground font-mono space-y-1 text-left">
            <div>
              Type:{" "}
              <span className="text-foreground">
                {workspaceSummary.variant}
              </span>
            </div>
            <div>
              Role:{" "}
              <span className="text-foreground">{workspaceSummary.role}</span>
            </div>
            <div>
              Members:{" "}
              <span className="text-foreground">
                {workspaceSummary.memberCount}
              </span>
            </div>
          </div>

          {workspaceSummary.description && (
            <div className="px-4 py-3 border-b border-border/50 text-xs text-muted-foreground font-mono text-left">
              {workspaceSummary.description}
            </div>
          )}
          <div className="px-4 py-3 flex gap-2">
            <Link to={"/workspaces/" + workspaceSummary.id}>
              <Button
                size="sm"
                className="h-8 px-3 text-xs font-mono bg-primary hover:bg-primary/90 text-primary-foreground border-0"
              >
                Open
              </Button>
            </Link>
            {workspaceSummary.canUpdateWorkspace ? (
              <Link to={"/workspaces/" + workspaceSummary.id + "/settings"}>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 px-3 text-xs font-mono border-border hover:bg-card bg-transparent"
                >
                  <Settings className="h-3 w-3 mr-1" />
                  Settings
                </Button>
              </Link>
            ) : (
              <Button
                size="sm"
                variant="outline"
                className="h-8 px-3 text-xs font-mono border-border bg-transparent"
                disabled
              >
                <Settings className="h-3 w-3 mr-1" />
                Settings
              </Button>
            )}
            {workspaceSummary.canManageMembers ? (
              <Link to={"/workspaces/" + workspaceSummary.id + "/members"}>
                <Button
                  size="sm"
                  variant="ghost"
                  className="h-8 px-3 text-xs font-mono text-muted-foreground hover:text-foreground hover:bg-transparent"
                >
                  <Users className="h-3 w-3 mr-1" />
                  Members
                </Button>
              </Link>
            ) : (
              <Button
                size="sm"
                variant="ghost"
                className="h-8 px-3 text-xs font-mono text-muted-foreground"
                disabled
              >
                <Users className="h-3 w-3 mr-1" />
                Members
              </Button>
            )}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}

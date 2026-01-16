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

type Props = {
  workspaceSummary: WorkspaceSummary;
  onOpen?: (id: string) => void;
  onPin?: (id: string) => void;
};

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

            {/* Main content - name only in collapsed */}
            <div className="flex-1 min-w-0">
              <h3 className="font-mono font-bold text-sm text-foreground truncate tracking-tight">
                {workspaceSummary.name}
              </h3>
            </div>

            <Button
              size="sm"
              variant="ghost"
              className="h-6 w-6 p-0 flex-shrink-0 text-muted-foreground hover:text-primary transition-colors"
              onClick={(e) => {
                e.stopPropagation();
                onPin?.(workspaceSummary.id);
              }}
            >
              <Pin
                className={cn(
                  "h-4 w-4",
                  workspaceSummary.isPinned && "fill-primary text-primary"
                )}
              />
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="h-6 w-6 p-0 flex-shrink-0 text-muted-foreground hover:text-primary transition-colors"
              onClick={(e) => {
                e.stopPropagation();
                onPin?.(workspaceSummary.id);
              }}
            >
              <PencilLine
                className={cn(
                  "h-4 w-4",
                  workspaceSummary.isPinned && "fill-primary text-primary"
                )}
              />
            </Button>

            {/* Chevron */}
            <ChevronDown
              className={cn(
                "h-4 w-4 text-muted-foreground transition-transform flex-shrink-0 group-hover:text-foreground",
                open && "rotate-180"
              )}
            />
          </div>
        </div>
      </CollapsibleTrigger>

      <CollapsibleContent>
        <div className="border border-t-0 border-border bg-card/50">
          <div className="px-4 py-3 border-b border-border/50 text-xs text-muted-foreground font-mono space-y-1">
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
            <div className="px-4 py-3 border-b border-border/50 text-xs text-muted-foreground font-mono">
              {workspaceSummary.description}
            </div>
          )}
          <div className="px-4 py-3 flex gap-2">
            <Link to={"/workspace/" + workspaceSummary.id} >
            <Button
              size="sm"
              className="h-8 px-3 text-xs font-mono bg-primary hover:bg-primary/90 text-primary-foreground border-0"
            >
              Open
            </Button>
            </Link>
            <Button
              size="sm"
              variant="outline"
              className="h-8 px-3 text-xs font-mono border-border hover:bg-card bg-transparent"
            >
              <Settings className="h-3 w-3 mr-1" />
              Settings
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="h-8 px-3 text-xs font-mono text-muted-foreground hover:text-foreground hover:bg-transparent"
            >
              <Users className="h-3 w-3 mr-1" />
              Members
            </Button>
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}

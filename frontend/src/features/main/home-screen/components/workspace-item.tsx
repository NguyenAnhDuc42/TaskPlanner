import type { WorkspaceSummary } from "../type";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Pin, Settings, Users } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { RoleBadge } from "@/components/role-badge";
import { Card } from "@/components/ui/card";

type Props = {
  workspaceSummary: WorkspaceSummary;
  onOpen?: (id: string) => void;
  onPin?: (id: string, isPinned: boolean) => void;
};

const truncate = (str: string, max: number) => {
  if (str.length <= max) return str;
  return str.slice(0, max) + "...";
};


export function WorkspaceItem({ workspaceSummary, onOpen, onPin, selected }: Props & { selected?: boolean }) {
  return (
    <Card
      className={cn(
        "group relative border transition-all duration-300 cursor-pointer p-4 overflow-hidden rounded-xl",
        selected 
          ? "border-opacity-50 border-solid shadow-md scale-[1.01]" 
          : "border-border/50 hover:border-primary/40 hover:bg-muted/30",
        "w-full max-w-full"
      )}
      style={selected ? {
        borderColor: workspaceSummary.color,
        backgroundColor: `${workspaceSummary.color}10`,
      } : undefined}
      onClick={() => onOpen?.(workspaceSummary.id)}
    >
      <div className="flex items-start gap-4 w-full">
        {/* Workspace Icon */}
        <div 
          className="h-12 w-12 rounded-sm flex items-center justify-center text-sm font-bold shrink-0 border border-border/50 transition-transform group-hover:scale-105 shadow-sm"
          style={{
            backgroundColor: `${workspaceSummary.color}15`,
            borderColor: `${workspaceSummary.color}40`,
          }}
        >
          <DynamicIcon
            name={workspaceSummary.icon}
            color={workspaceSummary.color}
            size={24}
          />
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0 py-0.5 overflow-hidden">
          <div className="flex items-center gap-2 mb-1 w-full overflow-hidden">
            <h3 className="font-bold text-sm text-foreground truncate tracking-tight flex-1">
              {truncate(workspaceSummary.name, 40)}
            </h3>
            <RoleBadge role={workspaceSummary.role} />
          </div>
          
          <p className="text-xs text-muted-foreground line-clamp-1 h-4 mb-3 italic font-medium opacity-60 max-w-full truncate">
            {workspaceSummary.description 
              ? truncate(workspaceSummary.description, 80) 
              : "No description set"}
          </p>

          <div className="flex items-center gap-4 text-muted-foreground/60">
            <div className="flex items-center gap-1.5 transition-colors group-hover:text-foreground">
              <Users className="h-3.5 w-3.5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">{workspaceSummary.memberCount}</span>
            </div>
            <div className="flex items-center gap-1.5 transition-colors group-hover:text-foreground/70">
              <div className="h-1 w-1 rounded-full bg-border" />
              <span className="text-[10px] font-mono uppercase tracking-widest">{workspaceSummary.variant}</span>
            </div>
          </div>
        </div>

        {/* Actions Container */}
        <div className="flex flex-col gap-1 shrink-0 transition-opacity">
           <Button
              size="icon"
              variant="ghost"
              className="h-8 w-8 text-muted-foreground hover:bg-transparent rounded-sm"
              disabled={!workspaceSummary.canPinWorkspace}
              onClick={(e) => {
                e.stopPropagation();
                onPin?.(workspaceSummary.id, !workspaceSummary.isPinned);
              }}
            >
              <Pin className={cn("h-3.5 w-3.5", workspaceSummary.isPinned && "fill-primary")} />
            </Button>
            <Button
              size="icon"
              variant="ghost"
              className="h-8 w-8 text-muted-foreground hover:bg-transparent rounded-sm"
              onClick={(e) => {
                e.stopPropagation();
              }}
            >
              <Settings className="h-3.5 w-3.5" />
            </Button>
        </div>
      </div>
      
      {/* Decorative pulse when selected */}
      {selected && (
        <div className="absolute top-0 right-0 p-1">
          <div className="h-1.5 w-1.5 rounded-full animate-pulse" style={{ backgroundColor: workspaceSummary.color }} />
        </div>
      )}
    </Card>
  );
}

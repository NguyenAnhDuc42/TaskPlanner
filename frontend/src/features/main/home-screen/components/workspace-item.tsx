import type { WorkspaceSummary } from "../type";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Pin, Settings, Users } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Badge } from "@/components/ui/badge";
import type { Role } from "@/types/role";
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

export function WorkspaceItem({ workspaceSummary, onOpen, onPin, selected }: Props & { selected?: boolean }) {
  return (
    <Card
      className={cn(
        "group relative border transition-all duration-300 cursor-pointer p-4 overflow-hidden",
        selected 
          ? "border-primary bg-primary/5 shadow-md shadow-primary/5 scale-[1.01]" 
          : "border-border/50 hover:border-primary/40 hover:bg-muted/30",
        "w-full max-w-full"
      )}
      onClick={() => onOpen?.(workspaceSummary.id)}
    >
      <div className="flex items-start gap-4 w-full">
        {/* Workspace Icon */}
        <div 
          className="h-12 w-12 rounded-xl flex items-center justify-center text-sm font-bold shrink-0 border border-border/50 transition-transform group-hover:scale-105 shadow-sm"
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
            {workspaceSummary.isPinned && (
              <Pin className="h-3 w-3 fill-primary text-primary animate-in fade-in zoom-in" />
            )}
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
        <div className="flex flex-col gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
           <Button
              size="icon"
              variant="ghost"
              className="h-8 w-8 text-muted-foreground hover:text-primary rounded-lg"
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
              className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-lg"
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
          <div className="h-1.5 w-1.5 rounded-full bg-primary animate-pulse" />
        </div>
      )}
    </Card>
  );
}

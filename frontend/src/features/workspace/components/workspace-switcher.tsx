import { useState } from "react";
import { 
  Popover, 
  PopoverContent, 
  PopoverTrigger 
} from "@/components/ui/popover";
import { ChevronDown, Check, Plus, } from "lucide-react";
import { cn } from "@/lib/utils";


import { useWorkspace } from "../context/workspace-provider";
import { Pin } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate } from "@tanstack/react-router";
import type { WorkspaceSummary } from "@/features/main/home-screen/type";
import { CreateWorkspaceForm } from "@/features/main/home-screen/components/create-workspace-form";
import { useSetWorkspacePin } from "@/features/main/home-screen/api";

export function WorkspaceSwitcher() {
  const [open, setOpen] = useState(false);
  const { workspaces, workspaceId } = useWorkspace();
  const navigate = useNavigate();
  const { mutate: setPin } = useSetWorkspacePin();

  const activeWorkspace = workspaces.find((ws: WorkspaceSummary) => ws.id === workspaceId) || workspaces[0];

  const [showCreateForm, setShowCreateForm] = useState(false);

  if (!activeWorkspace) return null;

  return (
    <>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <div className="flex items-center gap-2 px-1 py-1 rounded-sm hover:bg-muted transition-colors cursor-pointer group">
            <div 
              className="h-5 w-5 rounded flex items-center justify-center border border-white/10 shadow-sm"
              style={{ backgroundColor: activeWorkspace.color || "#6366f1" }}
            >
              <DynamicIcon name={activeWorkspace.icon} size={12} className="text-white" />
            </div>
            <span className="text-[11px] font-black uppercase tracking-tight opacity-70 group-hover:opacity-100 transition-opacity">
              {activeWorkspace.name}
            </span>
            <ChevronDown className={cn(
              "h-3 w-3 text-muted-foreground transition-transform duration-200",
              open && "rotate-180"
            )} />
          </div>
        </PopoverTrigger>
        <PopoverContent className="w-56 bg-background border border-border shadow-2xl rounded-sm" align="start">
          <div className="px-1">
            <span className="text-[9px] font-black uppercase tracking-widest text-muted-foreground/50">Workspaces</span>
          </div>
          <div className="space-y-0.5">
            {workspaces.map((ws: WorkspaceSummary) => {
              return (
                <button
                  key={ws.id}
                  onClick={() => {
                    navigate({ to: `/workspaces/${ws.id}` });
                    setOpen(false);
                  }}
                  className={cn(
                    "w-full flex items-center justify-between px-1.5 py-1 rounded-sm text-[11px] font-medium transition-colors",
                    workspaceId === ws.id 
                      ? "bg-primary/10 text-primary" 
                      : "hover:bg-muted text-muted-foreground hover:text-foreground"
                  )}
                >
                  <div className="flex items-center gap-2">
                    {/* Pin Icon on the left */}
                    <Pin 
                      className={cn(
                        "h-3 w-3 transition-colors cursor-pointer hover:text-primary",
                        ws.isPinned ? "text-primary fill-primary" : "text-muted-foreground/30"
                      )} 
                      onClick={(e) => {
                        e.stopPropagation(); // Prevent navigation
                        setPin({ workspaceId: ws.id, isPinned: !ws.isPinned });
                      }}
                    />

                    <div 
                      className="h-4 w-4 rounded-sm flex items-center justify-center text-[10px] font-black text-white uppercase shadow-sm"
                      style={{ backgroundColor: ws.color || "#6366f1" }}
                    >
                      <DynamicIcon name={ws.icon} size={10} />
                    </div>
                    {ws.name}
                  </div>
                  {workspaceId === ws.id && <Check className="h-3 w-3 text-primary" />}
                </button>
              );
            })}
          </div>
          <div className="h-px bg-border/50" />
          <button 
            className="w-full flex items-cente px-1.5 py-1 rounded-sm text-[11px] font-bold text-muted-foreground hover:bg-muted hover:text-foreground transition-colors uppercase tracking-tight"
            onClick={() => {
              setShowCreateForm(true);
              setOpen(false);
            }}
          >
            <Plus className="h-3.5 w-3.5" />
            Create Workspace
          </button>
        </PopoverContent>
      </Popover>

      <CreateWorkspaceForm 
        open={showCreateForm} 
        onOpenChange={setShowCreateForm}
      />
    </>
  );
}

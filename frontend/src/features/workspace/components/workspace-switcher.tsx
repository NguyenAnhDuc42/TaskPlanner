import { useState } from "react";
import { 
  Popover, 
  PopoverContent, 
  PopoverTrigger 
} from "@/components/ui/popover";
import { ChevronDown, Check, Plus, } from "lucide-react";
import { cn } from "@/lib/utils";


interface Workspace {
  id: string;
  name: string;
  color: string;
}

const MOCK_WORKSPACES: Workspace[] = [
  { id: "1", name: "Engineering", color: "#6366f1" },
  { id: "2", name: "Marketing", color: "#ec4899" },
  { id: "3", name: "Personal", color: "#10b981" },
];

export function WorkspaceSwitcher() {
  const [open, setOpen] = useState(false);
  const [activeWorkspace, setActiveWorkspace] = useState(MOCK_WORKSPACES[0]);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <div className="flex items-center gap-2 px-2 py-1 rounded-md hover:bg-muted transition-colors cursor-pointer group">
          <div 
            className="h-5 w-5 rounded flex items-center justify-center border border-white/10 shadow-sm"
            style={{ backgroundColor: activeWorkspace.color }}
          >
            <span className="text-[10px] font-black text-white uppercase">
              {activeWorkspace.name.substring(0, 1)}
            </span>
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
      <PopoverContent className="w-56 p-1 bg-background border border-border shadow-2xl rounded-xl" align="start">
        <div className="px-2 py-1.5 mb-1">
          <span className="text-[9px] font-black uppercase tracking-widest text-muted-foreground/50">Workspaces</span>
        </div>
        <div className="space-y-0.5">
          {MOCK_WORKSPACES.map((ws) => (
            <button
              key={ws.id}
              onClick={() => {
                setActiveWorkspace(ws);
                setOpen(false);
              }}
              className={cn(
                "w-full flex items-center justify-between px-2 py-1.5 rounded-lg text-[11px] font-medium transition-colors",
                activeWorkspace.id === ws.id 
                  ? "bg-primary/10 text-primary" 
                  : "hover:bg-muted text-muted-foreground hover:text-foreground"
              )}
            >
              <div className="flex items-center gap-2">
                <div 
                  className="h-4 w-4 rounded-sm"
                  style={{ backgroundColor: ws.color }}
                />
                {ws.name}
              </div>
              {activeWorkspace.id === ws.id && <Check className="h-3 w-3" />}
            </button>
          ))}
        </div>
        <div className="h-px bg-border/50 my-1" />
        <button className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-[11px] font-bold text-muted-foreground hover:bg-muted hover:text-foreground transition-colors uppercase tracking-tight">
          <Plus className="h-3.5 w-3.5" />
          Create Workspace
        </button>
      </PopoverContent>
    </Popover>
  );
}

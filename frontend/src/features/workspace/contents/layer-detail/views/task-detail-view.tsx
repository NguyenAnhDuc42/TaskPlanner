import * as Icons from "lucide-react";
import { DescriptionSection } from "../components/overview/description-section";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { StatusBadge } from "@/components/status-badge";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { 
  Calendar as CalendarIcon, 
  Users, 
  Layers, 
  Flag, 
  Box,
  ChevronRight
} from "lucide-react";

interface TaskDetailViewProps {
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
}

export function TaskDetailView({ viewData, draft, onChange }: TaskDetailViewProps) {
  const { registry } = useWorkspace();
  
  if (!viewData) return null;

  const IconComponent = (Icons as any)[draft.icon] || Icons.CheckCircle2;
  const entityColor = draft.color || "var(--primary)";

  // Resolve status and parent info
  const currentStatus = registry.statusMap[draft.statusId] || viewData.status;
  const parentName = viewData.parentFolderName || viewData.parentSpaceName || "Unknown";

  return (
    <div className="h-full overflow-y-auto no-scrollbar bg-background selection:bg-primary/20">
      <div className="max-w-4xl mx-auto w-full pt-12 px-12 space-y-10 pb-32 animate-in fade-in slide-in-from-bottom-4 duration-700">
        
        {/* --- BREADCRUMBS --- */}
        <nav className="flex items-center gap-2 text-[10px] font-black uppercase tracking-widest text-muted-foreground/30">
          <div className="flex items-center gap-1.5 hover:text-muted-foreground/60 transition-colors cursor-pointer">
            <Box className="h-3 w-3" />
            <span>{parentName}</span>
          </div>
          <ChevronRight className="h-2.5 w-2.5 opacity-30" />
          <span className="text-muted-foreground/60">Task Detail</span>
        </nav>

        {/* --- IDENTITY HEADER --- */}
        <header className="flex items-start gap-6">
          <Popover>
            <PopoverTrigger asChild>
              <button
                className="h-12 w-12 rounded-xl flex items-center justify-center border border-border/10 flex-shrink-0 transition-all hover:scale-105 active:scale-95 shadow-sm mt-1"
                style={{
                  backgroundColor: `${entityColor}15`,
                  color: entityColor,
                }}
              >
                <IconComponent className="h-6 w-6 stroke-[2.5px]" />
              </button>
            </PopoverTrigger>
            <PopoverContent
              className="w-auto p-0 border-none bg-transparent shadow-none"
              sideOffset={12}
              align="start"
            >
              <UniversalPicker
                selectedIcon={draft.icon}
                selectedColor={draft.color}
                onSelect={(icon, color) => onChange({ icon, color })}
              />
            </PopoverContent>
          </Popover>

          <textarea
            value={draft.name}
            onChange={(e) => onChange({ name: e.target.value })}
            className="flex-1 bg-transparent border-none outline-none text-4xl font-black tracking-tight text-foreground placeholder:text-muted-foreground/10 resize-none min-h-[48px] overflow-hidden"
            placeholder="What needs to be done?"
            spellCheck={false}
            rows={1}
            onInput={(e) => {
              const target = e.target as HTMLTextAreaElement;
              target.style.height = "auto";
              target.style.height = `${target.scrollHeight}px`;
            }}
          />
        </header>

        {/* --- QUICK ACTIONS / METADATA GRID --- */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-6 rounded-2xl bg-muted/20 border border-border/5">
          {/* Status */}
          <div className="space-y-2">
            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/40 flex items-center gap-2">
              <Layers className="h-3 w-3" />
              Status
            </label>
            <StatusBadge status={currentStatus} className="bg-background/50 hover:bg-background border-border/10 transition-colors cursor-pointer w-fit" />
          </div>

          {/* Priority */}
          <div className="space-y-2">
            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/40 flex items-center gap-2">
              <Flag className="h-3 w-3" />
              Priority
            </label>
            <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-background/50 border border-border/10 text-[10px] font-bold text-foreground/70 hover:bg-background transition-colors cursor-pointer w-fit">
              <div className={cn(
                "h-1.5 w-1.5 rounded-full",
                draft.priority >= 4 ? "bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.5)]" :
                draft.priority >= 3 ? "bg-amber-500" :
                draft.priority >= 2 ? "bg-blue-500" : "bg-slate-400"
              )} />
              {draft.priority === 4 ? "Urgent" : draft.priority === 3 ? "High" : draft.priority === 2 ? "Medium" : "Low"}
            </div>
          </div>

          {/* Assignees */}
          <div className="space-y-2">
            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/40 flex items-center gap-2">
              <Users className="h-3 w-3" />
              Assignees
            </label>
            <div className="flex -space-x-2">
              {viewData.members?.length > 0 ? (
                viewData.members.map((m: any, i: number) => (
                  <div key={i} className="h-6 w-6 rounded-full border-2 border-background bg-muted flex items-center justify-center text-[8px] font-bold uppercase overflow-hidden shadow-sm ring-1 ring-border/5">
                    {m.avatarUrl ? (
                      <img src={m.avatarUrl} alt={m.name} className="h-full w-full object-cover" />
                    ) : (
                      <span>{m.name?.[0] || "?"}</span>
                    )}
                  </div>
                ))
              ) : (
                <div className="h-6 w-6 rounded-full border border-dashed border-muted-foreground/30 flex items-center justify-center text-muted-foreground/30 hover:border-primary/50 hover:text-primary transition-all cursor-pointer">
                  <Plus className="h-3 w-3" />
                </div>
              )}
            </div>
          </div>

          {/* Dates */}
          <div className="space-y-2">
            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/40 flex items-center gap-2">
              <CalendarIcon className="h-3 w-3" />
              Schedule
            </label>
            <div className="text-[10px] font-black font-mono tracking-tight text-foreground/80 flex items-center gap-1.5 cursor-pointer hover:text-primary transition-colors">
              {draft.dueDate ? format(new Date(draft.dueDate), "MMM dd, yyyy") : "No Due Date"}
            </div>
          </div>
        </div>

        {/* --- DESCRIPTION AREA --- */}
        <div className="space-y-4">
          <div className="flex items-center gap-3">
            <h3 className="text-[11px] font-black uppercase tracking-[0.2em] text-muted-foreground/40">Description</h3>
            <div className="h-px flex-1 bg-border/5" />
          </div>
          <div className="pl-1 min-h-[200px]">
            <DescriptionSection
              documentId={viewData.defaultDocumentId}
            />
          </div>
        </div>

      </div>
    </div>
  );
}

function Plus({ className }: { className?: string }) {
  return <Icons.Plus className={className} />;
}

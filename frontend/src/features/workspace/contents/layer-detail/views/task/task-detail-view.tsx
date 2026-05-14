import * as Icons from "lucide-react";
import { DescriptionSection } from "../../components/overview/description-section";
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
  ChevronRight,
  Plus
} from "lucide-react";
import { Calendar } from "@/components/ui/calendar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { PriorityBadge } from "@/components/priority-badge";
import { Priority } from "@/types/priority";


interface TaskDetailViewProps {
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
}

export function TaskDetailView({ viewData, draft, onChange }: TaskDetailViewProps) {
  const { registry } = useWorkspace();
  
  if (!viewData) return null;

  // Use optional chaining to prevent crash when draft is null on first load
  const IconComponent = (Icons as any)[draft?.icon || viewData?.icon] || Icons.CheckCircle2;
  const entityColor = draft?.color || viewData?.color || "var(--primary)";

  // Resolve status and parent info
  const currentStatus = registry.statusMap[draft?.statusId] || viewData.status;
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
                selectedIcon={draft?.icon || viewData?.icon}
                selectedColor={draft?.color || viewData?.color}
                onSelect={(icon, color) => onChange({ icon, color })}
              />
            </PopoverContent>
          </Popover>

          <textarea
            value={draft?.name ?? viewData?.name ?? ""}
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

        {/* --- QUICK ACTIONS / METADATA ROW --- */}
        <div className="flex items-center gap-1.5 flex-wrap mt-2">
          {/* Status */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <div className="cursor-pointer">
                <StatusBadge status={currentStatus} className="text-[10px] font-bold" />
              </div>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
              {registry.workflows.find((w: any) => 
                w.id?.toLowerCase() === viewData.workflowId?.toLowerCase()
              )?.statuses.map((status: any) => (
                <DropdownMenuItem 
                  key={status.id}
                  onSelect={() => onChange({ statusId: status.id })}
                  className="p-1 rounded-lg cursor-pointer transition-colors"
                >
                  <StatusBadge status={status} className="w-full justify-start border-none bg-transparent hover:bg-muted/20" />
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Priority */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <div className="cursor-pointer">
                <PriorityBadge priority={draft?.priority || viewData?.priority} className="text-[10px] font-bold px-2.5 py-1" />
              </div>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
              {Object.values(Priority).map((p) => (
                <DropdownMenuItem 
                  key={p}
                  onSelect={() => onChange({ priority: p })}
                  className="p-1 rounded-lg cursor-pointer transition-colors"
                >
                  <PriorityBadge priority={p} className="w-full justify-start border-none bg-transparent hover:bg-muted/20" />
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Assignees */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Users className="h-3 w-3 stroke-[2.5px]" />
                <span>{viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-48 p-2 bg-background/95 border-border/40 shadow-2xl rounded-xl" align="start">
              <div className="text-[10px] font-black uppercase tracking-wider text-muted-foreground/40 mb-2 px-1">Assignees</div>
              <div className="space-y-1">
                {viewData.members?.map((m: any, i: number) => (
                  <div key={i} className="flex items-center gap-2 p-1 hover:bg-muted/50 rounded-md transition-colors">
                    <div className="h-5 w-5 rounded-full bg-muted flex items-center justify-center text-[8px] font-bold uppercase overflow-hidden">
                      {m.avatarUrl ? <img src={m.avatarUrl} alt={m.name} className="h-full w-full object-cover" /> : <span>{m.name?.[0] || "?"}</span>}
                    </div>
                    <span className="text-[10px] font-bold">{m.name}</span>
                  </div>
                ))}
              </div>
            </PopoverContent>
          </Popover>

          {/* Dates */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <CalendarIcon className="h-3 w-3 stroke-[2.5px]" />
                <span>{draft?.dueDate ? format(new Date(draft.dueDate), "MMM dd") : "No Due Date"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={draft?.dueDate ? new Date(draft.dueDate) : undefined}
                onSelect={(date) => onChange({ dueDate: date?.toISOString() })}
                initialFocus
              />
            </PopoverContent>
          </Popover>
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

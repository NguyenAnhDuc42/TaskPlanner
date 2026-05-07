import { 
  Layers, 
  Calendar as CalendarIcon, 
  CheckCircle2, 
  Users, 
  Lock,
  Globe,
  ArrowRight,
  ChevronDown
} from "lucide-react";
import { format } from "date-fns";
import { EntityLayerType } from "@/types/entity-layer-type";
import { cn } from "@/lib/utils";
import { useMemo, useState } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { TelemetrySection } from "./telemetry-section";

interface PropertySidebarProps {
  layerType: EntityLayerType;
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
}

export function PropertySidebar({ layerType, viewData, draft, onChange }: PropertySidebarProps) {
  const { registry } = useWorkspace();
  const [collapsed, setCollapsed] = useState({
    properties: false,
    telemetry: false
  });
  
  if (!viewData) return null;

  const isSpace = layerType === EntityLayerType.ProjectSpace;
  const isFolder = layerType === EntityLayerType.ProjectFolder;
  const isTask = layerType === EntityLayerType.ProjectTask;

  // Find workflow and its statuses
  const workflow = useMemo(() => {
    const wId = viewData.workflowId || viewData.parentWorkflowId;
    if (!wId) return null;
    return registry.workflows.find(w => w.id === wId);
  }, [viewData.workflowId, viewData.parentWorkflowId, registry.workflows]);

  const statuses = workflow?.statuses || [];
  
  // Resolve current status from draft
  const currentStatus = useMemo(() => {
    return registry.statusMap[draft.statusId] || viewData.status;
  }, [draft.statusId, viewData.status, registry.statusMap]);

  const toggleCollapse = (key: keyof typeof collapsed) => {
    setCollapsed(prev => ({ ...prev, [key]: !prev[key] }));
  };

  return (
    <div className="space-y-6">
      {/* --- Properties Section --- */}
      <section className="space-y-4">
        <button 
          onClick={() => toggleCollapse("properties")}
          className="w-full flex items-center justify-between group px-1"
        >
          <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 group-hover:text-muted-foreground/60 transition-colors">Properties</h3>
          <div className={cn("transition-transform duration-300", collapsed.properties && "-rotate-90")}>
            <ChevronDown className="h-3 w-3 text-muted-foreground/20" />
          </div>
        </button>

        {!collapsed.properties && (
          <div className="space-y-1.5 animate-in slide-in-from-top-2 duration-300">
            {/* 0. Visibility */}
            <div className=" py-1">
              <div className="flex bg-muted/20 p-0.5 rounded-md border border-border/10 w-full">
                  <button
                    onClick={() => onChange({ isPrivate: false })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      !draft.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Globe className="h-3 w-3" />
                    Public
                  </button>
                  <button
                    onClick={() => onChange({ isPrivate: true })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      draft.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Lock className="h-3 w-3" />
                    Private
                  </button>
              </div>
            </div>

            {/* 1. Status Picker - Hidden for tasks as it's in TaskDetailView */}
            {!isTask && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                      <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                        <Layers className="h-3.5 w-3.5" />
                        <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
                      </div>
                      <StatusBadge status={currentStatus} />
                  </div>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
                  <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                    <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Select Status</span>
                  </div>
                  
                  {/* No Status Option */}
                  <DropdownMenuItem 
                    onSelect={() => onChange({ statusId: null })}
                    className="p-1 rounded-lg cursor-pointer transition-colors"
                  >
                    <div className="flex items-center gap-2.5 px-3 py-1.5 w-full text-[10px] font-black uppercase tracking-widest text-muted-foreground/40 hover:text-muted-foreground/80 transition-colors">
                      <div className="h-1.5 w-1.5 rounded-full bg-muted-foreground/20" />
                      No Status
                    </div>
                  </DropdownMenuItem>

                  {statuses.map((status: any) => (
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
            )}

            {/* 2. Members */}
            <PropertyRow 
              icon={Users} 
              label={isTask ? "Assignees" : "Members"} 
              value={viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"} 
            />

            {/* 3. Schedule - Hidden for tasks as it's in TaskDetailView */}
            {!isTask && (
              <div className="group flex flex-col gap-2 py-2 px-1 hover:bg-muted/30 rounded-lg transition-all border border-transparent hover:border-border/10">
                  <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                    <CalendarIcon className="h-3.5 w-3.5" />
                    <span className="text-[10px] font-black uppercase tracking-wider">Schedule</span>
                  </div>
                  
                  <div className="flex items-center justify-between w-full bg-muted/40 px-3 py-1.5 rounded-md border border-border/5 group-hover:bg-muted/60 transition-all">
                    {/* Start Date */}
                    <Popover>
                      <PopoverTrigger asChild>
                        <button className="flex flex-col text-left hover:opacity-70 transition-opacity">
                          <span className="text-[7px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0">Start</span>
                          <span className="text-[10px] font-black text-foreground/80 font-mono tracking-tight">
                            {draft.startDate ? format(new Date(draft.startDate), "MM/dd/yyyy") : "TBD"}
                          </span>
                        </button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0" align="start">
                        <Calendar
                          mode="single"
                          selected={draft.startDate ? new Date(draft.startDate) : undefined}
                          onSelect={(date) => onChange({ startDate: date?.toISOString() })}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                    
                    <ArrowRight className="h-3 w-3 text-muted-foreground/20" />

                    {/* Due Date */}
                    <Popover>
                      <PopoverTrigger asChild>
                        <button className="flex flex-col items-end text-right hover:opacity-70 transition-opacity">
                          <span className="text-[7px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0">Due</span>
                          <span className="text-[10px] font-black text-foreground/80 font-mono tracking-tight">
                            {draft.dueDate ? format(new Date(draft.dueDate), "MM/dd/yyyy") : "TBD"}
                          </span>
                        </button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0" align="end">
                        <Calendar
                          mode="single"
                          selected={draft.dueDate ? new Date(draft.dueDate) : undefined}
                          onSelect={(date) => onChange({ dueDate: date?.toISOString() })}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
              </div>
            )}

            {/* 4. Workflow */}
            {(isSpace || isFolder) && workflow && (
              <PropertyRow icon={CheckCircle2} label="Workflow" value={workflow.name} />
            )}

          </div>
        )}
      </section>

      {/* --- Telemetry Section --- */}
      <section className="space-y-4">
        <button 
          onClick={() => toggleCollapse("telemetry")}
          className="w-full flex items-center justify-between group px-1"
        >
          <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 group-hover:text-muted-foreground/60 transition-colors">Telemetry</h3>
          <div className={cn("transition-transform duration-300", collapsed.telemetry && "-rotate-90")}>
            <ChevronDown className="h-3 w-3 text-muted-foreground/20" />
          </div>
        </button>

        {!collapsed.telemetry && (
          <div className="animate-in slide-in-from-top-2 duration-300">
            <TelemetrySection activities={viewData.recentActivity} />
          </div>
        )}
      </section>
    </div>
  );
}

function PropertyRow({ 
  icon: Icon, 
  label, 
  value, 
  color 
}: { 
  icon: any, 
  label: string, 
  value: string, 
  color?: string
}) {
  return (
    <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
      <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
        <Icon className="h-3.5 w-3.5" />
        <span className="text-[10px] font-bold uppercase tracking-wider">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {color && <div className="h-1.5 w-1.5 rounded-[2px]" style={{ backgroundColor: color }} />}
        <span className="text-[10px] font-black text-foreground/80 tracking-tight transition-colors group-hover:text-foreground truncate max-w-[120px]">
          {value}
        </span>
      </div>
    </div>
  );
}


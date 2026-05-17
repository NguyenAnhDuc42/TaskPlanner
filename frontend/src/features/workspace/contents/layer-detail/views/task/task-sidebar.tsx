import { 
  Layers, 
  Calendar as CalendarIcon, 
  Users, 
  ArrowRight,
  ChevronDown,
  Flag
} from "lucide-react";
import { format } from "date-fns";
import { cn } from "@/lib/utils";
import { useMemo, useState } from "react";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import { Priority } from "@/types/priority";
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
import { StatusSelect } from "@/components/status-select";
import { useTaskEditor } from "./task-editor-context";

export function TaskSidebar() {
  const { registry } = useWorkspace();
  const { task, updateField } = useTaskEditor();
  
  const [collapsed, setCollapsed] = useState({
    properties: false,
  });
  
  if (!task) return null;

  // Find workflow and its statuses
  const workflow = useMemo(() => {
    if (task.projectFolderId) {
      return registry.workflows.find((w: any) => 
        w.projectFolderId?.toLowerCase() === task.projectFolderId?.toLowerCase()
      );
    }
    if (task.projectSpaceId) {
      return registry.workflows.find((w: any) => 
        w.projectSpaceId?.toLowerCase() === task.projectSpaceId?.toLowerCase() && !w.projectFolderId
      );
    }
    return null;
  }, [task.projectFolderId, task.projectSpaceId, registry.workflows]);
  
  // Resolve current status from task context
  const currentStatus = useMemo(() => {
    return registry.statusMap[task.statusId || ""] || task.status;
  }, [task.statusId, task.status, registry.statusMap]);

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
            <StatusSelect
              value={task.statusId}
              onChange={(statusId) => updateField({ statusId })}
              workflowId={workflow?.id}
              align="end"
              trigger={
                <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                    <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                      <Layers className="h-3.5 w-3.5" />
                      <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
                    </div>
                    <StatusBadge status={currentStatus} />
                </div>
              }
            />

            {/* 3. Priority */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                    <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                      <Flag className="h-3.5 w-3.5" />
                      <span className="text-[10px] font-bold uppercase tracking-wider">Priority</span>
                    </div>
                    <PriorityBadge priority={task.priority} className="px-2 py-1 text-[10px]" />
                </div>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
                <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                  <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Select Priority</span>
                </div>
                
                {Object.values(Priority).map((p) => (
                  <DropdownMenuItem 
                    key={p}
                    onSelect={() => updateField({ priority: p })}
                    className="p-1 rounded-lg cursor-pointer transition-colors"
                  >
                    <PriorityBadge priority={p} className="w-full justify-start border-none bg-transparent hover:bg-muted/20" />
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

            {/* 4. Members (Assignees) */}
            <PropertyRow 
              icon={Users} 
              label="Assignees" 
              value={task.members?.length > 0 ? `${task.members.length} Users` : "No Members"} 
            />

            {/* 4. Schedule */}
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
                          {task.startDate ? format(new Date(task.startDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="start">
                      <Calendar
                        mode="single"
                        selected={task.startDate ? new Date(task.startDate) : undefined}
                        onSelect={(date) => updateField({ startDate: date?.toISOString() })}
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
                          {task.dueDate ? format(new Date(task.dueDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="end">
                      <Calendar
                        mode="single"
                        selected={task.dueDate ? new Date(task.dueDate) : undefined}
                        onSelect={(date) => updateField({ dueDate: date?.toISOString() })}
                        initialFocus
                      />
                    </PopoverContent>
                  </Popover>
                </div>
            </div>
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

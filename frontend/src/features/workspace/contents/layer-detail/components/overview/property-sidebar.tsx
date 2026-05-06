import { 
  Layers, 
  Calendar as CalendarIcon, 
  Clock, 
  CheckCircle2, 
  Flag, 
  Hash, 
  Users, 
  Lock,
  Globe,
  ArrowRight,
  ChevronDown
} from "lucide-react";
import { format } from "date-fns";
import { EntityLayerType } from "@/types/entity-layer-type";
import { cn } from "@/lib/utils";
import { useState, useEffect, useMemo } from "react";
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
import { useUpdateFolder, useUpdateSpace, useUpdateTask } from "../../layer-api";

interface PropertySidebarProps {
  layerType: EntityLayerType;
  viewData: any;
}

export function PropertySidebar({ layerType, viewData }: PropertySidebarProps) {
  const { registry, workspaceId } = useWorkspace();
  
  const updateSpace = useUpdateSpace(workspaceId);
  const updateFolder = useUpdateFolder(workspaceId);
  const updateTask = useUpdateTask(workspaceId);

  if (!viewData) return null;

  const isSpace = layerType === EntityLayerType.ProjectSpace;
  const isFolder = layerType === EntityLayerType.ProjectFolder;
  const isTask = layerType === EntityLayerType.ProjectTask;

  // Generic update handler
  const handleUpdate = (updates: any) => {
    if (isSpace) updateSpace.mutate({ spaceId: viewData.id, ...updates });
    if (isFolder) updateFolder.mutate({ folderId: viewData.id, ...updates });
    if (isTask) updateTask.mutate({ taskId: viewData.id, ...updates });
  };

  // Find workflow and its statuses
  const workflow = useMemo(() => {
    const wId = viewData.workflowId || viewData.parentWorkflowId;
    if (!wId) return null;
    return registry.workflows.find(w => w.id === wId);
  }, [viewData.workflowId, viewData.parentWorkflowId, registry.workflows]);

  const statuses = workflow?.statuses || [];

  return (
    <div className="space-y-8">
      <section className="space-y-4">
        <h3 className="text-[10px] font-black uppercase tracking-[0.2em] pl-1 text-muted-foreground/30">Properties</h3>

        <div className="space-y-1.5">
           {/* 0. Visibility */}
           <div className=" py-1">
             <div className="flex bg-muted/20 p-0.5 rounded-md border border-border/10 w-full">
                <button
                  onClick={() => handleUpdate({ isPrivate: false })}
                  className={cn(
                    "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                    !viewData.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Globe className="h-3 w-3" />
                  Public
                </button>
                <button
                  onClick={() => handleUpdate({ isPrivate: true })}
                  className={cn(
                    "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                    viewData.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Lock className="h-3 w-3" />
                  Private
                </button>
             </div>
           </div>

           {/* 1. Status Picker */}
           <DropdownMenu>
             <DropdownMenuTrigger asChild>
               <div className="flex items-center justify-between py-2 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                  <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                    <Layers className="h-3.5 w-3.5" />
                    <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <StatusBadge status={viewData.status} />
                    <ChevronDown className="h-3 w-3 text-muted-foreground/20 group-hover:text-muted-foreground/40" />
                  </div>
               </div>
             </DropdownMenuTrigger>
             <DropdownMenuContent align="end" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
               <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                 <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Select Status</span>
               </div>
               {statuses.map((status: any) => (
                 <DropdownMenuItem 
                   key={status.id}
                   onSelect={() => handleUpdate({ statusId: status.id })}
                   className="flex items-center gap-2 px-2 py-1.5 rounded-lg cursor-pointer"
                 >
                   <div className="h-2 w-2 rounded-full" style={{ backgroundColor: status.color }} />
                   <span className="text-[10px] font-bold uppercase tracking-wider">{status.name}</span>
                 </DropdownMenuItem>
               ))}
             </DropdownMenuContent>
           </DropdownMenu>

           {/* 2. Workflow */}
           {(isSpace || isFolder) && workflow && (
             <PropertyRow icon={CheckCircle2} label="Workflow" value={workflow.name} />
           )}

           {/* 3. Members */}
           <PropertyRow 
            icon={Users} 
            label={isTask ? "Assignees" : "Members"} 
            value={viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"} 
           />

           {/* 4. Schedule (DatePicker) */}
           <div className="group flex flex-col gap-2.5 py-3 px-1 hover:bg-muted/30 rounded-lg transition-all border border-transparent hover:border-border/10">
              <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                <CalendarIcon className="h-3.5 w-3.5" />
                <span className="text-[10px] font-black uppercase tracking-wider">Schedule Timeline</span>
              </div>
              
              <div className="flex items-center justify-between w-full bg-muted/40 px-3 py-2 rounded-md border border-border/5 group-hover:bg-muted/60 transition-all">
                {/* Start Date */}
                <Popover>
                  <PopoverTrigger asChild>
                    <button className="flex flex-col text-left hover:opacity-70 transition-opacity">
                      <span className="text-[8px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0.5">Start</span>
                      <span className="text-[11px] font-black text-foreground/80 font-mono tracking-tight">
                        {viewData.startDate ? format(new Date(viewData.startDate), "MM/dd/yyyy") : "TBD"}
                      </span>
                    </button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                      mode="single"
                      selected={viewData.startDate ? new Date(viewData.startDate) : undefined}
                      onSelect={(date) => handleUpdate({ startDate: date?.toISOString() })}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
                
                <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/20" />

                {/* Due Date */}
                <Popover>
                  <PopoverTrigger asChild>
                    <button className="flex flex-col items-end text-right hover:opacity-70 transition-opacity">
                      <span className="text-[8px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0.5">Due</span>
                      <span className="text-[11px] font-black text-foreground/80 font-mono tracking-tight">
                        {viewData.dueDate ? format(new Date(viewData.dueDate), "MM/dd/yyyy") : "TBD"}
                      </span>
                    </button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="end">
                    <Calendar
                      mode="single"
                      selected={viewData.dueDate ? new Date(viewData.dueDate) : undefined}
                      onSelect={(date) => handleUpdate({ dueDate: date?.toISOString() })}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </div>
           </div>

           <div className="h-px w-full bg-border/5 my-2" />

           {/* Task Specific - Meta Properties */}
           {isTask && (
             <>
               {/* Priority Picker */}
               <DropdownMenu>
                 <DropdownMenuTrigger asChild>
                   <div className="flex items-center justify-between py-2 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                     <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                       <Flag className="h-3.5 w-3.5" />
                       <span className="text-[10px] font-bold uppercase tracking-wider">Priority</span>
                     </div>
                     <div className="flex items-center gap-2">
                       {viewData.priority > 0 && (
                         <div 
                           className="h-1.5 w-1.5 rounded-full shadow-[0_0_8px_rgba(0,0,0,0.2)]" 
                           style={{ backgroundColor: getPriorityColor(viewData.priority) }} 
                         />
                       )}
                       <span className="text-[11px] font-black text-foreground/80 tracking-tight">
                         {getPriorityLabel(viewData.priority)}
                       </span>
                       <ChevronDown className="h-3 w-3 text-muted-foreground/20 group-hover:text-muted-foreground/40" />
                     </div>
                   </div>
                 </DropdownMenuTrigger>
                 <DropdownMenuContent align="end" className="w-40 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
                   {[4, 3, 2, 1, 0].map((p) => (
                     <DropdownMenuItem 
                       key={p}
                       onSelect={() => handleUpdate({ priority: p })}
                       className="flex items-center gap-2 px-2 py-1.5 rounded-lg cursor-pointer"
                     >
                       <Flag className="h-3 w-3" style={{ color: getPriorityColor(p) || "currentColor" }} />
                       <span className="text-[10px] font-bold uppercase tracking-wider">{getPriorityLabel(p)}</span>
                     </DropdownMenuItem>
                   ))}
                 </DropdownMenuContent>
               </DropdownMenu>

               <PropertyRow icon={Hash} label="Points" value={viewData.storyPoints?.toString() || "0"} />
               {viewData.timeEstimateSeconds && (
                 <PropertyRow icon={Clock} label="Estimate" value={formatSeconds(viewData.timeEstimateSeconds)} />
               )}
             </>
           )}
        </div>
      </section>

      <section className="space-y-4">
        <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 px-1">Recent Telemetry</h3>
        <div className="space-y-4 px-1">
          {viewData.recentActivity?.slice(0, 5).map((activity: any) => (
            <ActivityRow key={activity.id} label={activity.content} time={format(new Date(activity.timestamp), "h:mm a")} />
          )) || <div className="text-[10px] text-muted-foreground/20 italic font-bold">No recent telemetry</div>}
        </div>
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
    <div className="flex items-center justify-between py-2 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
      <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
        <Icon className="h-3.5 w-3.5" />
        <span className="text-[10px] font-bold uppercase tracking-wider">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {color && <div className="h-1.5 w-1.5 rounded-full shadow-[0_0_8px_rgba(0,0,0,0.2)]" style={{ backgroundColor: color }} />}
        <span className="text-[11px] font-black text-foreground/80 tracking-tight transition-colors group-hover:text-foreground truncate max-w-[120px]">
          {value}
        </span>
      </div>
    </div>
  );
}

function ActivityRow({ label, time }: { label: string, time: string }) {
  return (
    <div className="flex items-center justify-between group py-1">
      <div className="flex items-center gap-3 overflow-hidden">
        <div className="h-1 w-1 rounded-full bg-muted-foreground/10 group-hover:bg-primary transition-colors flex-shrink-0" />
        <span className="text-[10px] font-bold text-muted-foreground/60 group-hover:text-foreground transition-colors truncate">{label}</span>
      </div>
      <span className="text-[10px] font-mono text-muted-foreground/20 flex-shrink-0">{time}</span>
    </div>
  );
}

function getPriorityLabel(priority?: number) {
  switch (priority) {
    case 4: return 'Urgent';
    case 3: return 'High';
    case 2: return 'Medium';
    case 1: return 'Low';
    default: return 'None';
  }
}

function getPriorityColor(priority?: number) {
  switch (priority) {
    case 4: return '#ef4444'; // Urgent
    case 3: return '#f97316'; // High
    case 2: return '#eab308'; // Medium
    case 1: return '#3b82f6'; // Low
    default: return undefined;
  }
}

function formatSeconds(seconds: number) {
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
}

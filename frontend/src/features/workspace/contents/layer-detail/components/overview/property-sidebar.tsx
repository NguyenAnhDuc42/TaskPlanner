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
  ArrowRight
} from "lucide-react";
import { format } from "date-fns";
import { EntityLayerType } from "@/types/entity-layer-type";
import { cn } from "@/lib/utils";
import { useState, useEffect, useMemo } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";

interface PropertySidebarProps {
  layerType: EntityLayerType;
  viewData: any;
}

export function PropertySidebar({ layerType, viewData }: PropertySidebarProps) {
  const { registry } = useWorkspace();
  const [isPrivate, setIsPrivate] = useState(viewData?.isPrivate || false);
  
  useEffect(() => {
    setIsPrivate(viewData?.isPrivate || false);
  }, [viewData?.isPrivate]);

  if (!viewData) return null;

  const isSpace = layerType === EntityLayerType.ProjectSpace;
  const isTask = layerType === EntityLayerType.ProjectTask;

  // Find workflow name from registry if it's a space/folder
  const workflowName = useMemo(() => {
    if (!viewData.workflowId) return null;
    return registry.workflows.find(w => w.id === viewData.workflowId)?.name;
  }, [viewData.workflowId, registry.workflows]);

  return (
    <div className="space-y-8">
      <section className="space-y-4">
        <h3 className="text-[10px] font-black uppercase tracking-[0.2em] pl-1 text-muted-foreground/30">Properties</h3>

        <div className="space-y-1.5">
           {/* 0. Visibility */}
           <div className=" py-1">
             <div className="flex bg-muted/20 p-0.5 rounded-md border border-border/10 w-full">
                <button
                  onClick={() => setIsPrivate(false)}
                  className={cn(
                    "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                    !isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Globe className="h-3 w-3" />
                  Public
                </button>
                <button
                  onClick={() => setIsPrivate(true)}
                  className={cn(
                    "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                    isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Lock className="h-3 w-3" />
                  Private
                </button>
             </div>
           </div>

           {/* 1. Status */}
           <div className="flex items-center justify-between py-2 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
              <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                <Layers className="h-3.5 w-3.5" />
                <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
              </div>
              <StatusBadge status={viewData.status} />
           </div>

           {/* 2. Workflow */}
           {(isSpace || layerType === EntityLayerType.ProjectFolder) && workflowName && (
             <PropertyRow icon={CheckCircle2} label="Workflow" value={workflowName} />
           )}

           {/* 3. Members */}
           <PropertyRow 
            icon={Users} 
            label={isTask ? "Assignees" : "Members"} 
            value={viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"} 
           />

           {/* 4. Schedule */}
           <div className="group flex flex-col gap-2.5 py-3 px-1 hover:bg-muted/30 rounded-lg transition-all border border-transparent hover:border-border/10">
              <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                <CalendarIcon className="h-3.5 w-3.5" />
                <span className="text-[10px] font-black uppercase tracking-wider">Schedule Timeline</span>
              </div>
              <div className="flex items-center justify-between w-full bg-muted/40 px-3 py-2 rounded-md border border-border/5 group-hover:bg-muted/60 transition-all">
                <div className="flex flex-col">
                  <span className="text-[8px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0.5">Start</span>
                  <span className="text-[11px] font-black text-foreground/80 font-mono tracking-tight">{viewData.startDate ? format(new Date(viewData.startDate), "MM/dd/yyyy") : "TBD"}</span>
                </div>
                
                <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/20" />

                <div className="flex flex-col items-end">
                  <span className="text-[8px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0.5">Due</span>
                  <span className="text-[11px] font-black text-foreground/80 font-mono tracking-tight">{viewData.dueDate ? format(new Date(viewData.dueDate), "MM/dd/yyyy") : "TBD"}</span>
                </div>
              </div>
           </div>

           <div className="h-px w-full bg-border/5 my-2" />

           {/* Task Specific - Meta Properties */}
           {isTask && (
             <>
               <PropertyRow 
                icon={Flag} 
                label="Priority" 
                value={getPriorityLabel(viewData.priority)} 
                color={getPriorityColor(viewData.priority)} 
               />
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

import { FileText, Clock, Trash2, Archive, MoreVertical } from "lucide-react";
import { Button } from "@/components/ui/button";



interface TaskInfoViewProps {
  data: ViewResponse;
  taskId: string;
}

export function TaskInfoView({ data, taskId: _taskId }: TaskInfoViewProps) {
  return (
    <div className="h-full flex flex-col space-y-8 p-8 max-w-4xl mx-auto">
      {/* Header Info */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2 text-primary">
            <FileText className="h-4 w-4" />
            <span className="text-[10px] font-black uppercase tracking-[0.2em]">Task Overview</span>
          </div>
          <h1 className="text-3xl font-black tracking-tighter text-foreground/90">
            {data.tasks?.[0]?.name || "Draft Objective"}
          </h1>
        </div>
        <div className="flex items-center gap-2">
           <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground/40 hover:text-foreground border border-transparent hover:border-white/5">
             <Archive className="h-4 w-4" />
           </Button>
           <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground/40 hover:text-destructive border border-transparent hover:border-destructive/10">
             <Trash2 className="h-4 w-4" />
           </Button>
           <div className="w-px h-4 bg-white/5 mx-2" />
           <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground/40 hover:text-foreground border border-transparent hover:border-white/5">
             <MoreVertical className="h-4 w-4" />
           </Button>
        </div>
      </div>

      {/* Content Area */}
      <div className="flex-1 space-y-10 group">
        <div className="space-y-4">
           <div className="flex items-center gap-2 text-muted-foreground/30">
              <span className="text-[10px] font-black uppercase tracking-widest">Description</span>
              <div className="h-px flex-1 bg-white/5" />
           </div>
           <div className="text-sm leading-relaxed text-muted-foreground/70 font-medium">
             This is a specialized information view for the individual task. It allows for high-density reading and focus on the task's specific documentation and core objectives.
             <br /><br />
             Unlike Space and Folder views that show lists of tasks, this view is a "deep dive" into the mission-critical parameters of a single task.
           </div>
        </div>

        {/* Mock Activity Stream */}
        <div className="space-y-4 opacity-50 group-hover:opacity-100 transition-opacity">
           <div className="flex items-center gap-2 text-muted-foreground/30">
              <span className="text-[10px] font-black uppercase tracking-widest">Recent Telemetry</span>
              <div className="h-px flex-1 bg-white/5" />
           </div>
           <div className="space-y-4">
              <ActivityRow icon={Clock} text="System established the initial parameters" time="2h ago" />
              <ActivityRow icon={FileText} text="Documentation was expanded by Nguyen Anh Duc" time="45m ago" />
           </div>
        </div>
      </div>
    </div>
  );
}

import type { LucideIcon } from "lucide-react";
import type { ViewResponse } from "../../views-type";

function ActivityRow({ icon: Icon, text, time }: { icon: LucideIcon, text: string, time: string }) {
  return (
    <div className="flex items-center justify-between py-1 group/row cursor-default">
      <div className="flex items-center gap-3">
        <div className="p-1 rounded-md bg-white/5 text-muted-foreground/40 group-hover/row:text-primary transition-colors">
          <Icon className="h-3 w-3" />
        </div>
        <span className="text-[11px] font-medium text-muted-foreground/60">{text}</span>
      </div>
      <span className="text-[10px] font-mono text-muted-foreground/20">{time}</span>
    </div>
  );
}

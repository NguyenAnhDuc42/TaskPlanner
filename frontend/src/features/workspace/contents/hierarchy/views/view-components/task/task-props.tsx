import { CheckCircle2, User, Flag, Clock } from "lucide-react";

interface TaskPropsProps {
  entityId: string;
}

export function TaskProps({ entityId: _entityId }: TaskPropsProps) {
  return (
    <div className="space-y-4">
       <div className="p-3 rounded-xl bg-orange-500/5 border border-orange-500/10 space-y-2">
        <div className="flex items-center gap-2 text-orange-500">
          <CheckCircle2 className="h-3.5 w-3.5" />
          <span className="text-[10px] font-black uppercase tracking-widest">Task Details</span>
        </div>
        <div className="flex items-center justify-between py-1">
           <span className="text-[10px] font-medium text-muted-foreground/40">Status</span>
           <div className="px-2 py-0.5 rounded-full bg-orange-500/10 text-[9px] font-bold text-orange-400">
              In Progress
           </div>
        </div>
      </div>

       <div className="space-y-2">
          <PropRow icon={Flag} label="Priority" value="High" />
          <PropRow icon={User} label="Assignee" value="Nguyen Anh Duc" />
          <PropRow icon={Clock} label="Due Date" value="Apr 10, 2026" />
       </div>
    </div>
  );
}

function PropRow({ icon: Icon, label, value }: { icon: any, label: string, value: string }) {
  return (
    <div className="flex items-center justify-between group py-0.5">
      <div className="flex items-center gap-2">
        <Icon className="h-3 w-3 text-muted-foreground/30" />
        <span className="text-[10px] font-medium text-muted-foreground/40">{label}</span>
      </div>
      <span className="text-[10px] font-bold text-muted-foreground/70">{value}</span>
    </div>
  );
}

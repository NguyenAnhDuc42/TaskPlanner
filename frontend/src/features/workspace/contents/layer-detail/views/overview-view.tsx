import { RichTextEditor } from "@/components/rich-text-editor";
import { FileText, BarChart3, Activity, Layers, Calendar as CalendarIcon, Clock, CheckCircle2 } from "lucide-react";
import { format } from "date-fns";

interface OverviewViewProps {
  entityInfo: any;
  viewData: any;
}

export function OverviewView({ entityInfo, viewData }: OverviewViewProps) {
  if (!entityInfo || !viewData) return null;

  // Snatch progress from viewData (legacy EntityOverviewContext logic)
  const progress = viewData.progress || { completedTasks: 0, totalTasks: 0 };
  const progressPercentage = progress.totalTasks > 0 
    ? (progress.completedTasks / progress.totalTasks) * 100 
    : 0;

  return (
    <div className="h-full overflow-y-auto px-8 py-10 no-scrollbar">
      <div className="max-w-6xl mx-auto w-full grid grid-cols-1 lg:grid-cols-12 gap-12">
        
        {/* --- Left Column: Scope & Documentation --- */}
        <div className="lg:col-span-8 space-y-12">
          {/* Quick Status Banner */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <OverviewCard icon={BarChart3} label="Progress" value={`${Math.round(progressPercentage)}%`} detail={`${progress.completedTasks}/${progress.totalTasks} Done`} />
            <OverviewCard icon={Layers} label="Status" value={viewData.status?.name || "No Status"} detail="Current Phase" color={viewData.status?.color} />
            <OverviewCard icon={Activity} label="Activity" value={viewData.recentActivity?.length || "0"} detail="Recent Events" />
          </div>

          {/* Description / Scope Area */}
          <section className="space-y-4">
            <div className="flex items-center gap-2 text-muted-foreground/30">
              <FileText className="h-3 w-3" />
              <span className="text-[10px] font-black uppercase tracking-[0.2em]">Operational Scope</span>
            </div>
            <div className="min-h-[400px] p-6 rounded-xl border border-border/40 bg-muted/5 shadow-inner">
              <RichTextEditor
                value={entityInfo.description || ""}
                onChange={() => {}} // Handle change later
                placeholder="Define the strategic objectives and operational scope for this layer..."
              />
            </div>
          </section>
        </div>

        {/* --- Right Column: Properties & Context (Snatched from legacy) --- */}
        <div className="lg:col-span-4 space-y-10">
          <section className="space-y-6">
            <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 px-2">Node Properties</h3>
            <div className="space-y-1">
               <PropertyRow icon={Layers} label="Status" value={viewData.status?.name || "None"} color={viewData.status?.color} />
               <PropertyRow icon={CalendarIcon} label="Start Date" value={viewData.startDate ? format(new Date(viewData.startDate), "MMM d, yyyy") : "None"} />
               <PropertyRow icon={Clock} label="Due Date" value={viewData.dueDate ? format(new Date(viewData.dueDate), "MMM d, yyyy") : "None"} />
               {viewData.workflowName && (
                 <PropertyRow icon={CheckCircle2} label="Workflow" value={viewData.workflowName} />
               )}
            </div>
          </section>

          <section className="space-y-6">
            <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 px-2">Recent Telemetry</h3>
            <div className="space-y-4 px-2">
              {viewData.recentActivity?.slice(0, 5).map((activity: any) => (
                <ActivityRow key={activity.id} label={activity.content} time={format(new Date(activity.timestamp), "h:mm a")} />
              )) || <div className="text-[10px] text-muted-foreground/20 italic font-bold">No recent telemetry</div>}
            </div>
          </section>
        </div>

      </div>
    </div>
  );
}

function OverviewCard({ icon: Icon, label, value, detail, color }: { icon: any, label: string, value: string, detail: string, color?: string }) {
  return (
    <div className="bg-muted/5 border border-border/30 rounded-lg p-4 group hover:bg-muted/10 transition-colors">
      <div className="flex items-center gap-2 mb-2">
        <Icon className="h-3 w-3 text-muted-foreground/30 group-hover:text-primary transition-colors" />
        <span className="text-[9px] font-bold uppercase tracking-widest text-muted-foreground/50">{label}</span>
      </div>
      <div 
        className="text-xl font-black tracking-tight text-foreground"
        style={{ color: color }}
      >
        {value}
      </div>
      <div className="text-[9px] font-medium text-muted-foreground/30 uppercase mt-0.5">{detail}</div>
    </div>
  );
}

function PropertyRow({ icon: Icon, label, value, color }: { icon: any, label: string, value: string, color?: string }) {
  return (
    <div className="flex items-center justify-between py-2.5 px-3 hover:bg-muted/30 rounded-lg transition-colors group cursor-default">
      <div className="flex items-center gap-2.5">
        <Icon className="h-3.5 w-3.5 text-muted-foreground/30 group-hover:text-primary transition-colors" />
        <span className="text-[11px] font-semibold text-muted-foreground/60">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {color && <div className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: color }} />}
        <span className="text-[11px] font-bold text-foreground/80 tracking-tight">{value}</span>
      </div>
    </div>
  );
}

function ActivityRow({ label, time }: { label: string, time: string }) {
  return (
    <div className="flex items-center justify-between group">
      <div className="flex items-center gap-3">
        <div className="h-1 w-1 rounded-full bg-muted-foreground/10 group-hover:bg-primary transition-colors" />
        <span className="text-[10px] font-bold text-muted-foreground/60 group-hover:text-foreground transition-colors truncate max-w-[180px]">{label}</span>
      </div>
      <span className="text-[10px] font-mono text-muted-foreground/20">{time}</span>
    </div>
  );
}

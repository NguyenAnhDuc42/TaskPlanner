
import { LayoutDashboard, Target, Zap, Activity } from "lucide-react";

export function CommandCenterView() {
  return (
    <div className="flex-1 p-6 space-y-6 overflow-auto">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-black tracking-tight text-foreground uppercase">Command Center</h1>
          <p className="text-muted-foreground text-sm font-mono uppercase tracking-widest mt-1">Operational Overview & System Status</p>
        </div>
        <div className="flex items-center gap-2">
           <div className="h-2 w-2 rounded-full bg-green-500 animate-pulse" />
           <span className="text-[10px] font-bold uppercase tracking-tighter text-muted-foreground">System Online</span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard title="Active Projects" value="12" icon={LayoutDashboard} trend="+2 this week" />
        <StatCard title="Pending Tasks" value="48" icon={Target} trend="-5% from yesterday" />
        <StatCard title="Velocity" value="2.4" icon={Zap} trend="Optimal" />
        <StatCard title="Health" value="98%" icon={Activity} trend="Stable" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-background border border-border rounded-xl p-6 shadow-sm">
          <h3 className="text-sm font-bold uppercase tracking-widest mb-4">Priority Heatmap</h3>
          <div className="h-64 bg-muted/30 rounded-lg border border-dashed border-border flex items-center justify-center text-xs text-muted-foreground font-mono">
             [ Visualization Data Layer ]
          </div>
        </div>
        <div className="bg-background border border-border rounded-xl p-6 shadow-sm">
          <h3 className="text-sm font-bold uppercase tracking-widest mb-4">Resource Allocation</h3>
          <div className="h-64 bg-muted/30 rounded-lg border border-dashed border-border flex items-center justify-center text-xs text-muted-foreground font-mono">
             [ Resource Distribution Map ]
          </div>
        </div>
      </div>
    </div>
  );
}

function StatCard({ title, value, icon: Icon, trend }: { title: string, value: string, icon: any, trend: string }) {
  return (
    <div className="bg-background border border-border rounded-xl p-4 shadow-sm hover:border-primary/50 transition-colors group cursor-default">
      <div className="flex items-center justify-between mb-2">
        <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground group-hover:text-primary transition-colors">{title}</span>
        <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
      </div>
      <div className="text-2xl font-black text-foreground">{value}</div>
      <div className="text-[10px] font-bold text-muted-foreground mt-1 uppercase tracking-tighter">{trend}</div>
    </div>
  );
}

import { 
  LayoutGrid, 
  AlertCircle, 
  Search, 
  ArrowRight,
  FolderOpen,
  Layers,
  History,
  Terminal,
  Zap
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function CommandCenterIndex({ isFallback }: { isFallback?: boolean }) {
  return (
    <div className="flex-1 h-full flex flex-col bg-background overflow-hidden select-none">
      {/* --- Hub Header --- */}
      <div className="px-6 py-8 border-b border-border/5">
        <div className="flex items-center gap-3 mb-2">
          <div className="h-2 w-2 rounded-full bg-primary animate-pulse shadow-[0_0_8px_rgba(var(--primary),0.5)]" />
          <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Workspace Operational Hub</span>
        </div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground flex items-center gap-3">
          {isFallback ? "Location Not Found" : "Systems Overview"}
          <span className="px-1.5 py-0.5 rounded bg-muted/30 text-[9px] font-bold uppercase tracking-widest text-muted-foreground border border-border/50">
            v1.0.4-stable
          </span>
        </h1>
        {isFallback && (
          <p className="mt-2 text-sm text-muted-foreground max-w-md">
            The resource you're looking for might have been moved, deleted, or is temporarily unavailable in the current hierarchy.
          </p>
        )}
      </div>

      <div className="flex-1 overflow-auto px-6 py-8">
        <div className="max-w-6xl w-full grid grid-cols-1 lg:grid-cols-12 gap-8">
          
          {/* --- Left Column: Primary Stats & Actions --- */}
          <div className="lg:col-span-8 space-y-10">
            
            {/* Quick Navigation Grid */}
            <section>
              <h3 className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground/40 mb-4 flex items-center gap-2">
                <LayoutGrid className="h-3 w-3" />
                Active Directives
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <HubCard 
                  title="Global Task Queue" 
                  description="View all unassigned and active tasks across the entire workspace."
                  icon={Layers}
                  count="142"
                />
                <HubCard 
                  title="Orphaned Objects" 
                  description="Manage items whose parent layers have been modified or deleted."
                  icon={AlertCircle}
                  count="7"
                  variant="warning"
                />
              </div>
            </section>

            {/* Global Search / Command Entry */}
            <section className="bg-muted/10 border border-border/50 rounded-lg p-6 group hover:bg-muted/20 transition-all cursor-text">
               <div className="flex items-center gap-4 text-muted-foreground group-hover:text-foreground transition-colors">
                 <Search className="h-5 w-5" />
                 <div className="flex-1">
                   <div className="text-sm font-semibold mb-0.5">Quick Command Center</div>
                   <div className="text-xs opacity-50">Search everything or run global workspace actions...</div>
                 </div>
                 <div className="flex items-center gap-1">
                    <span className="px-1.5 py-0.5 rounded border border-border/50 bg-background text-[10px] font-bold">⌘</span>
                    <span className="px-1.5 py-0.5 rounded border border-border/50 bg-background text-[10px] font-bold">K</span>
                 </div>
               </div>
            </section>

            {/* Workspace Pulse Area */}
            <section>
               <h3 className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground/40 mb-4 flex items-center gap-2">
                <History className="h-3 w-3" />
                Workspace Pulse
              </h3>
              <div className="border border-border/50 rounded-lg divide-y divide-border/30 bg-muted/5 overflow-hidden">
                <ActivityRow label="Hierarchy sync completed" time="2m ago" />
                <ActivityRow label="API Node: Space creation successful" time="15m ago" />
                <ActivityRow label="System cleanup triggered" time="1h ago" />
                <ActivityRow label="Migration to layer protocol v2" time="4h ago" />
              </div>
            </section>
          </div>

          {/* --- Right Column: Utility & Meta --- */}
          <div className="lg:col-span-4 space-y-10">
            
            {/* System Status / Meta Info */}
            <section className="bg-background border border-border/50 rounded-lg p-5">
              <h3 className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground/40 mb-4">Node Metadata</h3>
              <div className="space-y-4">
                <MetaItem label="Active Nodes" value="28" />
                <MetaItem label="Latency" value="14ms" />
                <MetaItem label="Protocol" value="REST/v2" />
                <div className="h-px bg-border/30 my-4" />
                <div className="flex items-center justify-between text-[10px] font-bold uppercase tracking-wider text-green-500">
                  <span>Environment</span>
                  <div className="flex items-center gap-1.5">
                    <div className="h-1.5 w-1.5 rounded-full bg-green-500" />
                    Production
                  </div>
                </div>
              </div>
            </section>

            {/* Quick Links */}
            <section className="space-y-2">
               <h3 className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground/40 mb-4">Core Procedures</h3>
               <QuickLink icon={FolderOpen} label="Workspace Settings" />
               <QuickLink icon={Terminal} label="System Logs" />
               <QuickLink icon={Zap} label="Integration Matrix" />
            </section>
          </div>

        </div>
      </div>
    </div>
  );
}

function HubCard({ title, description, icon: Icon, count, variant }: { 
  title: string, 
  description: string, 
  icon: any, 
  count: string,
  variant?: "warning" | "default" 
}) {
  return (
    <div className="group bg-background border border-border/50 rounded-lg p-5 hover:border-primary/50 hover:bg-primary/[0.02] transition-all cursor-pointer shadow-sm">
      <div className="flex items-start justify-between mb-3">
        <div className={cn(
          "p-2 rounded-md",
          variant === "warning" ? "bg-orange-500/10 text-orange-500" : "bg-muted/50 text-muted-foreground group-hover:bg-primary/10 group-hover:text-primary transition-colors"
        )}>
          <Icon className="h-4 w-4" />
        </div>
        <span className={cn(
          "text-xl font-bold tracking-tight",
          variant === "warning" ? "text-orange-500" : "text-foreground"
        )}>{count}</span>
      </div>
      <h4 className="text-sm font-semibold mb-1 group-hover:text-primary transition-colors">{title}</h4>
      <p className="text-xs text-muted-foreground leading-relaxed">
        {description}
      </p>
    </div>
  );
}

function ActivityRow({ label, time }: { label: string, time: string }) {
  return (
    <div className="flex items-center justify-between px-4 py-3 group hover:bg-background/50 transition-colors">
      <div className="flex items-center gap-3">
        <div className="h-1 w-1 rounded-full bg-muted-foreground/30 group-hover:bg-primary transition-colors" />
        <span className="text-xs text-muted-foreground font-medium group-hover:text-foreground transition-colors">{label}</span>
      </div>
      <span className="text-[10px] font-mono text-muted-foreground/40">{time}</span>
    </div>
  );
}

function MetaItem({ label, value }: { label: string, value: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground/60">{label}</span>
      <span className="text-xs font-mono font-bold text-foreground">{value}</span>
    </div>
  );
}

function QuickLink({ icon: Icon, label }: { icon: any, label: string }) {
  return (
    <button className="w-full flex items-center justify-between px-3 py-2 rounded-md hover:bg-muted/40 transition-colors group">
      <div className="flex items-center gap-3">
        <Icon className="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary transition-colors" />
        <span className="text-xs font-medium text-muted-foreground group-hover:text-foreground transition-colors">{label}</span>
      </div>
      <ArrowRight className="h-3 w-3 text-muted-foreground/0 group-hover:text-primary group-hover:text-muted-foreground/100 transition-all -translate-x-2 group-hover:translate-x-0" />
    </button>
  );
}

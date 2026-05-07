import { 
  Tag, 
  Calendar, 
  Clock, 
  User, 
  Shield, 
  ChevronRight,
  X
} from "lucide-react";
import { Button } from "@/components/ui/button";

interface OverviewPanelProps {
  entityInfo: any;
  workspaceId: string;
  onClose: () => void;
}

export function OverviewPanel({ entityInfo, onClose }: OverviewPanelProps) {
  return (
    <div className="h-full flex flex-col bg-background select-none">
      <header className="h-11 px-4 border-b border-border/40 flex items-center justify-between flex-shrink-0">
        <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Properties</span>
        <Button variant="ghost" size="icon" className="h-7 w-7 rounded-md" onClick={onClose}>
          <X className="h-3.5 w-3.5" />
        </Button>
      </header>

      <div className="flex-1 overflow-auto p-4 space-y-8">
        {/* --- Identification --- */}
        <section className="space-y-4">
          <h4 className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/30">System ID</h4>
          <div className="flex items-center gap-3 px-2 py-1.5 rounded bg-muted/20 border border-border/10">
            <Tag className="h-3 w-3 text-muted-foreground/40" />
            <span className="text-[11px] font-mono text-foreground/80">{entityInfo.id.slice(0, 12)}</span>
          </div>
        </section>

        {/* --- Properties Grid --- */}
        <section className="space-y-4">
          <h4 className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/30">Configuration</h4>
          <div className="space-y-1">
            <PropRow icon={Shield} label="Privacy" value="Public" />
            <PropRow icon={User} label="Custodian" value="Nguyen Anh Duc" />
            <PropRow icon={Calendar} label="Created" value="May 02, 2026" />
            <PropRow icon={Clock} label="Modified" value="2m ago" />
          </div>
        </section>

        {/* --- Description --- */}
        <section className="space-y-4">
          <h4 className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/30">Operational Brief</h4>
          <div className="text-[11px] text-muted-foreground leading-relaxed bg-muted/5 p-3 rounded border border-border/20 italic">
            {entityInfo.description || "No strategic overview provided for this operational node."}
          </div>
        </section>

        {/* --- Hierarchy Link --- */}
        <section className="space-y-4">
          <h4 className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/30">Upstream</h4>
          <div className="flex items-center gap-2 text-[10px] font-bold text-primary hover:underline cursor-pointer">
            <ChevronRight className="h-3 w-3" />
            {entityInfo.parentName}
          </div>
        </section>
      </div>
      
      <footer className="p-4 border-t border-border/40">
         <div className="text-[9px] font-bold text-muted-foreground/20 uppercase tracking-widest text-center">
           Hierarchy v1.0.4 Node
         </div>
      </footer>
    </div>
  );
}

function PropRow({ icon: Icon, label, value }: { icon: any, label: string, value: string }) {
  return (
    <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded transition-colors group cursor-default">
      <div className="flex items-center gap-2.5">
        <Icon className="h-3.5 w-3.5 text-muted-foreground/30 group-hover:text-primary transition-colors" />
        <span className="text-[11px] font-semibold text-muted-foreground/60">{label}</span>
      </div>
      <span className="text-[11px] font-bold text-foreground/80">{value}</span>
    </div>
  );
}

import { Settings, Shield, Zap } from "lucide-react";

interface SpacePropsProps {
  entityId: string;
}

export function SpaceProps({ entityId: _entityId }: SpacePropsProps) {
  return (
    <div className="space-y-4">
      <div className="p-3 rounded-xl bg-primary/5 border border-primary/10 space-y-2">
        <div className="flex items-center gap-2 text-primary">
          <Zap className="h-3.5 w-3.5" />
          <span className="text-[10px] font-black uppercase tracking-widest">Space Power-ups</span>
        </div>
        <div className="flex flex-wrap gap-1">
          <Badge label="Automations" />
          <Badge label="Custom Fields" />
          <Badge label="Sprints" />
        </div>
      </div>

      <div className="space-y-2">
        <PropRow icon={Shield} label="Privacy" value="Private Space" />
        <PropRow icon={Settings} label="Type" value="Engineering" />
      </div>
    </div>
  );
}

function Badge({ label }: { label: string }) {
  return (
    <span className="px-2 py-0.5 rounded-full bg-white/5 border border-white/5 text-[9px] font-bold text-muted-foreground/60">
      {label}
    </span>
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

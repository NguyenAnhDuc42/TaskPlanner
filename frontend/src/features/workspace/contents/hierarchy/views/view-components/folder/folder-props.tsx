import { Folder, ArrowUpRight } from "lucide-react";

interface FolderPropsProps {
  entityId: string;
}

export function FolderProps({ entityId: _entityId }: FolderPropsProps) {
  return (
    <div className="space-y-4">
       <div className="p-3 rounded-xl bg-orange-500/5 border border-orange-500/10 space-y-2">
        <div className="flex items-center gap-2 text-orange-500">
          <Folder className="h-3.5 w-3.5" />
          <span className="text-[10px] font-black uppercase tracking-widest">Folder Details</span>
        </div>
        <div className="flex items-center justify-between py-1">
           <span className="text-[10px] font-medium text-muted-foreground/40">Parent Space</span>
           <div className="flex items-center gap-1.5 text-[10px] font-bold text-muted-foreground/70 hover:text-orange-400 cursor-pointer transition-colors">
              Engineering <ArrowUpRight className="h-3 w-3" />
           </div>
        </div>
      </div>

       <div className="space-y-2">
          <PropRow label="Status" value="Planning" />
          <PropRow label="Task Count" value="12" />
          <PropRow label="Visibility" value="Inherited" />
       </div>
    </div>
  );
}

function PropRow({ label, value }: { label: string, value: string }) {
  return (
    <div className="flex items-center justify-between group py-0.5">
      <span className="text-[10px] font-medium text-muted-foreground/40">{label}</span>
      <span className="text-[10px] font-bold text-muted-foreground/70">{value}</span>
    </div>
  );
}

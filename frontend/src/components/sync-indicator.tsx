import { Loader2, Check } from "lucide-react";
import { cn } from "@/lib/utils";

interface SyncIndicatorProps {
  isSaving: boolean;
  isDirty: boolean;
  className?: string;
}

export function SyncIndicator({ isSaving, isDirty, className }: SyncIndicatorProps) {
  return (
    <div className={cn("flex items-center", className)}>
      {isSaving ? (
        <div className="flex items-center gap-2 px-2 py-1 rounded-md bg-primary/10 border border-primary/10 transition-all duration-300">
           <Loader2 className="h-2.5 w-2.5 animate-spin text-primary" />
           <span className="text-[9px] font-black uppercase tracking-widest text-primary/80">Saving</span>
        </div>
      ) : isDirty ? (
        <div className="flex items-center gap-2 px-2 py-1 rounded-md bg-orange-500/10 border border-orange-500/10 animate-in fade-in zoom-in duration-300">
           <div className="h-2 w-2 rounded-[2px] bg-orange-500 animate-pulse" />
           <span className="text-[9px] font-black uppercase tracking-widest text-orange-500/80">Draft</span>
        </div>
      ) : (
        <div className="flex items-center gap-2 px-2 py-1 rounded-md bg-emerald-500/10 border border-emerald-500/10 opacity-60 hover:opacity-100 transition-all duration-300">
           <Check className="h-2.5 w-2.5 text-emerald-500 stroke-[3]" />
           <span className="text-[9px] font-black uppercase tracking-widest text-emerald-500/80">Synced</span>
        </div>
      )}
    </div>
  );
}

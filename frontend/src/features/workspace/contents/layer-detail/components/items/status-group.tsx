import { MoreHorizontal, Plus } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface StatusGroupProps {
  statusName: string;
  color: string;
  totalCount: number;
  children: ReactNode;
}

export function StatusGroup({
  statusName,
  color,
  totalCount,
  children,
}: StatusGroupProps) {
  return (
    <div 
      className="w-[300px] flex-shrink-0 flex flex-col h-full bg-[#080808]/40 rounded-xl border border-white/[0.03] shadow-2xl backdrop-blur-md overflow-hidden transition-all duration-300"
    >
      {/* Column Header */}
      <div className="flex items-center justify-between px-3 py-3 group/header border-b border-white/[0.02] bg-white/[0.01]">
        <div className="flex items-center gap-3">
          {/* Status Badge */}
          <div className="flex items-center gap-2 px-2 py-1 rounded-lg bg-white/[0.03] border border-white/[0.05] shadow-sm">
            <div
              className="w-1.5 h-1.5 rounded-full"
              style={{ backgroundColor: color, boxShadow: `0 0 10px ${color}88` }}
            />
            <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-foreground/80">
              {statusName}
            </h3>
          </div>
          
          {/* Count Badge */}
          <span className="text-[9px] font-black text-muted-foreground/40 px-2 py-0.5 rounded-full bg-white/[0.02] border border-white/[0.03]">
            {totalCount}
          </span>
        </div>
        
        {/* Actions */}
        <div className="flex items-center gap-0.5 opacity-0 group-hover/header:opacity-100 transition-all duration-200">
          <button className="p-1.5 hover:bg-white/5 rounded-lg text-muted-foreground/30 hover:text-foreground transition-all active:scale-90">
            <Plus className="h-3.5 w-3.5" />
          </button>
          <button className="p-1.5 hover:bg-white/5 rounded-lg text-muted-foreground/30 hover:text-foreground transition-all active:scale-90">
            <MoreHorizontal className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* Items Area */}
      <div className="flex-1 overflow-y-auto px-2 pb-4 pt-3 space-y-2 no-scrollbar">
        {children}
        
        {/* Drop Zone Placeholder */}
        <div className="group/drop flex flex-col items-center justify-center py-8 rounded-xl border-2 border-dashed border-white/[0.02] hover:border-white/[0.08] hover:bg-white/[0.01] transition-all duration-300 cursor-pointer">
           <div className="h-8 w-8 rounded-full border border-dashed border-white/[0.05] flex items-center justify-center mb-2 group-hover/drop:scale-110 transition-transform">
              <Plus className="h-3 w-3 text-white/[0.05] group-hover/drop:text-white/20" />
           </div>
           <span className="text-[9px] font-black text-white/[0.03] uppercase tracking-[0.2em] group-hover/drop:text-white/10 transition-colors">
              Drop here
           </span>
        </div>
      </div>
      
      {/* Progress Line (Optional aesthetic touch) */}
      <div 
        className="h-0.5 w-full opacity-30"
        style={{ 
          background: `linear-gradient(90deg, transparent 0%, ${color} 50%, transparent 100%)` 
        }} 
      />
    </div>
  );
}

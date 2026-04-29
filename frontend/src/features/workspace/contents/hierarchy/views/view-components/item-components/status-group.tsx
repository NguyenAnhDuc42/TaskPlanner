import { MoreHorizontal, Plus } from "lucide-react";
import type { ReactNode } from "react";

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
      className="w-[280px] flex-shrink-0 flex flex-col h-full bg-[#080808]/20 rounded-md border"
      style={{ borderColor: `${color}33` }} // 20% opacity of status color
    >
      {/* Column Header */}
      <div className="flex items-center justify-between p-3 pb-2 group">
        <div className="flex items-center gap-2.5">
          <div
            className="w-1.5 h-1.5 rounded-full"
            style={{ backgroundColor: color, boxShadow: `0 0 8px ${color}66` }}
          />
          <h3 className="text-[10px] font-black uppercase tracking-[0.15em] text-foreground/70">
            {statusName}
          </h3>
          <span className="text-[9px] font-bold text-muted-foreground/30 px-1.5 py-0.5 rounded-sm bg-white/[0.02]">
            {totalCount}
          </span>
        </div>
        
        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button className="p-1 hover:bg-white/5 rounded text-muted-foreground/40 hover:text-foreground transition-colors">
            <Plus className="h-3 w-3" />
          </button>
          <button className="p-1 hover:bg-white/5 rounded text-muted-foreground/40 hover:text-foreground transition-colors">
            <MoreHorizontal className="h-3 w-3" />
          </button>
        </div>
      </div>

      {/* Scrollable Card Container */}
      <div className="flex-1 overflow-y-auto px-1.5 pb-3 space-y-1.5 no-scrollbar">
        {children}
        
        {/* Empty state placeholder */}
        <div className="h-16 rounded-sm border border-dashed border-white/[0.01] flex items-center justify-center opacity-0 hover:opacity-100 transition-opacity group/drop">
          <span className="text-[8px] font-bold text-muted-foreground/10 uppercase tracking-widest group-hover/drop:text-muted-foreground/20 transition-colors">
            Drop here
          </span>
        </div>
      </div>
    </div>
  );
}

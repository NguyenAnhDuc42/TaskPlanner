import { MoreHorizontal, Plus } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";
import { StatusBadge } from "@/components/status-badge";

interface StatusGroupProps {
  id: string;
  statusName: string;
  color: string;
  totalCount: number;
  children: ReactNode;
  className?: string;
}

export function StatusGroup({
  statusName,
  color,
  totalCount,
  children,
  className,
}: StatusGroupProps) {
  return (
    <div 
      className={cn(
       "flex-shrink-0 flex flex-col bg-background rounded-md border border-white/[0.03] shadow-2xl overflow-hidden transition-all duration-300",
       className
      )}
    >
      {/* Column Header */}
      <div className="flex items-center justify-between px-3 py-3 group/header border-b border-white/[0.02] bg-white/[0.01]">
        <div className="flex items-center gap-3">
          <StatusBadge status={{ name: statusName, color: color } as any} />
          <span className="text-[9px] font-black text-muted-foreground/40 px-2 py-0.5 rounded-md bg-white/[0.02] border border-white/[0.03]">
            {totalCount}
          </span>
        </div>
        <div className="flex items-center gap-0.5 opacity-0 group-hover/header:opacity-100 transition-all duration-200">
          <button className="p-1.5 hover:bg-white/5 rounded-md text-muted-foreground/30 hover:text-foreground transition-all active:scale-90">
            <Plus className="h-3.5 w-3.5" />
          </button>
          <button className="p-1.5 hover:bg-white/5 rounded-md text-muted-foreground/30 hover:text-foreground transition-all active:scale-90">
            <MoreHorizontal className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* Items Area */}
      <div className="flex-1 px-2 pb-4 pt-3 no-scrollbar flex flex-col">
        {children}
      </div>
      
      <div 
        className="h-0.5 w-full opacity-30"
        style={{ 
          background: `linear-gradient(90deg, transparent 0%, ${color} 50%, transparent 100%)` 
        }} 
      />
    </div>
  );
}
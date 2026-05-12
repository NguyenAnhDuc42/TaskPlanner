import { MoreHorizontal, Plus } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";
import { StatusBadge } from "@/components/status-badge";
import { ScrollArea } from "@/components/ui/scroll-area";

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
       "flex-shrink-0 flex flex-col bg-transparent rounded-lg border border-border/40 overflow-hidden transition-all duration-300",
       className
      )}
    >
      {/* Column Header */}
      <div className="flex items-center justify-between px-3 py-2 group/header border-b border-border/10 bg-transparent">
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
      <div className="flex-1 px-2 pb-1 pt-3 flex flex-col min-h-0">
        {children}
      </div>
    </div>
  );
}
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
  if (totalCount === 0) return null;

  return (
    <div className="space-y-6">
      {/* Status Header */}
      <div className="flex items-center gap-4 pb-2 border-b border-border/20">
        <div
          className="w-1.5 h-1.5 rounded-full"
          style={{ backgroundColor: color }}
        />
        <span className="text-[10px] font-black uppercase tracking-[0.3em] text-foreground/80">
          {statusName}
        </span>
        <span className="text-[9px] font-bold text-muted-foreground/20 uppercase tracking-widest">
          {totalCount} Items
        </span>
      </div>

      {/* Items List Container */}
      <div className="space-y-1">{children}</div>
    </div>
  );
}

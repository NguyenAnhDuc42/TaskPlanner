import type { ReactNode } from "react";
import { ScrollArea } from "@/components/ui/scroll-area";

interface ExplorerColumnProps {
  title: string;
  count?: number;
  children: ReactNode;
}

export function ExplorerColumn({
  title,
  count,
  children,
}: ExplorerColumnProps) {
  return (
    <div className="flex-1 flex flex-col h-full bg-muted/5">
      {/* Small Header for flat status-less lists */}
      <div className="h-10 px-6 flex items-center justify-between border-b border-border/10">
        <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/40 shrink-0">
          {title}
        </span>
        {count !== undefined && (
          <span className="text-[9px] font-bold text-muted-foreground/20 uppercase">
            {count} Items
          </span>
        )}
      </div>

      <ScrollArea className="flex-1">
        <div className="p-6 space-y-1">
          {children}
        </div>
      </ScrollArea>
    </div>
  );
}

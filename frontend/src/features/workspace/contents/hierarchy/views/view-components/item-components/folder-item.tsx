import { Folder as FolderIcon, MoreHorizontal, Layers, Clock, Calendar } from "lucide-react";
import { cn } from "@/lib/utils";
import type { FolderItemDto } from "../../views-type";
import { format } from "date-fns";

interface FolderItemProps {
  folder: FolderItemDto;
  onClick: (folder: FolderItemDto) => void;
  isSelected?: boolean;
}

export function FolderItem({ folder, onClick, isSelected }: FolderItemProps) {
  // Mock ID for visual consistency
  const displayId = `FLDR-${folder.id.slice(0, 4).toUpperCase()}`;

  return (
    <div
      onClick={() => onClick(folder)}
      className={cn(
        "group flex flex-col gap-2 p-2.5 rounded-md transition-all cursor-pointer select-none active:scale-[0.98]",
        "border bg-[#0c0c0c] hover:bg-[#111111] shadow-sm",
        isSelected
          ? "border-primary/50 bg-[#141414] ring-1 ring-primary/10"
          : "border-white/[0.04] hover:border-white/[0.08]"
      )}
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5">
          <div className="p-1 rounded-[3px] bg-primary/10 text-primary">
            <FolderIcon className="h-2.5 w-2.5 fill-current" />
          </div>
          <span className="text-[8px] font-black text-muted-foreground/30 tracking-widest uppercase">
            {displayId}
          </span>
        </div>
        <MoreHorizontal className="h-3 w-3 text-muted-foreground/20 opacity-0 group-hover:opacity-100 transition-opacity" />
      </div>

      <div className="flex flex-col gap-1.5">
        <span className="text-[12px] font-bold text-foreground/80 group-hover:text-foreground transition-colors">
          {folder.name}
        </span>
        
        {/* Time stuff under the name */}
        <div className="flex items-center gap-3">
          {folder.startDate && (
            <div className="flex items-center gap-1 text-muted-foreground/40">
              <Calendar className="h-2.5 w-2.5" />
              <span className="text-[9px] font-bold uppercase tracking-tight">
                {format(new Date(folder.startDate), "MMM d")}
              </span>
            </div>
          )}
          {folder.dueDate && (
            <div className="flex items-center gap-1 text-muted-foreground/40">
              <Clock className="h-2.5 w-2.5" />
              <span className="text-[9px] font-bold uppercase tracking-tight">
                {format(new Date(folder.dueDate), "MMM d")}
              </span>
            </div>
          )}
          {!folder.startDate && !folder.dueDate && (
             <div className="flex items-center gap-1 text-muted-foreground/10 text-[8px] font-black uppercase tracking-widest">
                <Layers className="h-2 w-2" />
                <span>Drill down</span>
             </div>
          )}
        </div>
      </div>
    </div>
  );
}

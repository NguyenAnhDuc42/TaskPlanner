import { Folder as FolderIcon, MoreHorizontal, Layers } from "lucide-react";
import { cn } from "@/lib/utils";
import type { FolderItemDto } from "../../views-type";

interface FolderItemProps {
  folder: FolderItemDto;
  onClick: (folder: FolderItemDto) => void;
  isSelected?: boolean;
}

export function FolderItem({ folder, onClick, isSelected }: FolderItemProps) {
  return (
    <div
      onClick={() => onClick(folder)}
      className={cn(
        "group flex flex-col gap-2 p-2.5 rounded-md transition-all cursor-pointer select-none active:scale-[0.98]",
        "border bg-[#0a0a0a]/50 border-dashed border-white/[0.08] hover:bg-[#111111] hover:border-primary/30",
        isSelected
          ? "border-primary/50 bg-primary/[0.03] shadow-md shadow-primary/5"
          : "hover:shadow-md hover:shadow-black/10"
      )}
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5">
          <div className="p-1 rounded-sm bg-primary/10 text-primary">
            <FolderIcon className="h-2.5 w-2.5 fill-current" />
          </div>
          <span className="text-[8px] font-black text-muted-foreground/30 tracking-widest uppercase">
            Project Folder
          </span>
        </div>
        <MoreHorizontal className="h-3 w-3 text-muted-foreground/20 opacity-0 group-hover:opacity-100 transition-opacity" />
      </div>

      <div className="flex flex-col gap-0">
        <span className="text-[12px] font-bold text-foreground/70 group-hover:text-foreground transition-colors">
          {folder.name}
        </span>
        <div className="flex items-center gap-1 text-muted-foreground/20 text-[8px] font-bold uppercase tracking-wider">
          <Layers className="h-2 w-2" />
          <span>Drill down</span>
        </div>
      </div>
    </div>
  );
}

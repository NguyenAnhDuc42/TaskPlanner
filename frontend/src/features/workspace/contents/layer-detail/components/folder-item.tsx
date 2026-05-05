import { MoreHorizontal, Folder as FolderIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import type { FolderItemDto } from "../layer-detail-types";

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
        "border bg-[#0c0c0c] hover:bg-[#111111] shadow-sm",
        isSelected
          ? "border-primary/50 bg-[#141414] ring-1 ring-primary/10"
          : "border-white/[0.04] hover:border-white/[0.08]"
      )}
    >
      <div className="flex items-center justify-between">
        <span className="text-[9px] font-black text-muted-foreground/30 tracking-wider group-hover:text-muted-foreground/50 transition-colors uppercase">
          Folder
        </span>
        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <MoreHorizontal className="h-3 w-3 text-muted-foreground/30 hover:text-foreground" />
        </div>
      </div>

      <div className="flex items-center gap-2">
        <div 
          className="shrink-0 h-4 w-4 rounded flex items-center justify-center border shadow-inner"
          style={{ 
            backgroundColor: `${folder.color || "#3b82f6"}10`,
            borderColor: `${folder.color || "#3b82f6"}30`,
            color: folder.color || "#3b82f6" 
          }}
        >
          {folder.icon ? <span className="text-[9px] font-black">{folder.icon}</span> : <FolderIcon className="h-2.5 w-2.5" />}
        </div>
        <span className="text-[12px] font-bold leading-tight text-foreground/80 group-hover:text-foreground transition-colors line-clamp-1">
          {folder.name}
        </span>
      </div>
      
      <div className="mt-1 flex items-center gap-2">
         <div className="h-0.5 flex-1 bg-border/20 rounded-full overflow-hidden">
            <div className="h-full bg-primary/40 w-1/4 rounded-full" />
         </div>
      </div>
    </div>
  );
}

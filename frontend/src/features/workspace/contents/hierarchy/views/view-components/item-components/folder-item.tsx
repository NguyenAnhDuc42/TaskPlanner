import { Folder as FolderIcon } from "lucide-react";
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
        "group flex items-center gap-4 p-3 rounded-xl transition-all cursor-pointer active:scale-[0.99]",
        isSelected
          ? "bg-primary/5 border border-primary/10 shadow-sm"
          : "hover:bg-muted/50 border border-transparent"
      )}
    >
      <div className="p-1.5 rounded-lg bg-primary/10 text-primary group-hover:bg-primary/20 transition-colors">
        <FolderIcon className="h-3.5 w-3.5 fill-current" />
      </div>
      <span className="text-[14px] font-bold text-foreground/70 group-hover:text-foreground transition-colors">
        {folder.name}
      </span>
      <span className="ml-auto text-[9px] font-black text-muted-foreground/20 uppercase tracking-widest opacity-0 group-hover:opacity-100 transition-opacity">
        Click to Drill-Down
      </span>
    </div>
  );
}

import { Circle, Package, User } from "lucide-react";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import type { FolderItemDto } from "../../layer-detail-types";

interface FolderItemProps {
  folder: FolderItemDto;
  onClick: (folder: FolderItemDto) => void;
  isSelected?: boolean;
}

export function FolderItem({ folder, onClick, isSelected }: FolderItemProps) {
  const folderColor = folder.color || "#3b82f6";

  return (
    <div
      onClick={() => onClick(folder)}
      className={cn(
        "group relative flex flex-col gap-2 p-3 rounded-lg transition-all duration-200 cursor-pointer select-none",
        "border bg-[#0d0d0e]/80 hover:bg-[#161618] border-border/20 hover:border-border/40",
        isSelected && "border-primary/40 bg-[#121212]"
      )}
    >
      {/* 1. ID and Avatar Row */}
      <div className="flex items-center justify-between text-[11px] text-muted-foreground/40 font-medium">
        <span>{`FLD-${folder.id.slice(0, 4).toUpperCase()}`}</span>
        <div className="h-4 w-4 rounded-full bg-white/5 flex items-center justify-center border border-white/10">
          <User className="h-2.5 w-2.5 opacity-40" />
        </div>
      </div>

      {/* 2. Status & Title Row */}
      <div className="flex items-center gap-2">
        <div 
          className="shrink-0 h-4 w-4 flex items-center justify-center"
          style={{ color: folderColor }}
        >
          {folder.icon ? (
            <span className="text-[11px] font-bold">{folder.icon}</span>
          ) : (
            <Circle className="h-3 w-3" />
          )}
        </div>
        <h4 className="text-[12px] font-medium leading-tight text-foreground/90 group-hover:text-foreground transition-colors truncate">
          {folder.name}
        </h4>
      </div>

      {/* 3. Placeholders Row (Three dots & Box) */}
      <div className="flex items-center gap-1.5 mt-0.5">
        <div className="text-muted-foreground/30 text-[12px]">...</div>
        <div className="flex items-center gap-1 px-1.5 py-0.5 rounded bg-white/5 border border-white/5 text-muted-foreground/40 text-[10px]">
          <Package className="h-2.5 w-2.5" />
          <span>1</span>
        </div>
      </div>

      {/* 4. Date Row */}
      <div className="text-[10px] text-muted-foreground/30 mt-0.5">
        {`Created ${format(new Date(folder.createdAt), "MMMM d")}`}
      </div>
    </div>
  );
}

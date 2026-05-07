import { MoreHorizontal, Folder as FolderIcon, Layers } from "lucide-react";
import { cn } from "@/lib/utils";
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
        "group relative flex flex-col gap-3 p-3 rounded-xl transition-all duration-300 cursor-pointer select-none active:scale-[0.98]",
        "border bg-[#0a0a0a] hover:bg-[#0f0f0f] shadow-lg",
        isSelected
          ? "border-primary/40 bg-[#121212] ring-1 ring-primary/5"
          : "border-white/[0.03] hover:border-white/[0.08] hover:shadow-primary/[0.02]"
      )}
    >
      {/* Top Metadata Row */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5">
           <div className="flex items-center gap-2 px-2 py-0.5 rounded-md bg-white/[0.03] border border-white/[0.05] text-[8px] font-black text-muted-foreground/40 tracking-widest uppercase group-hover:text-muted-foreground/60 transition-colors">
              <Layers className="h-2 w-2" />
              Folder
           </div>
           
           {/* Color Indicator */}
           <div 
              className="h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: folderColor, boxShadow: `0 0 8px ${folderColor}66` }}
           />
        </div>
        
        <button className="p-1 opacity-0 group-hover:opacity-100 transition-all hover:bg-white/5 rounded-md">
          <MoreHorizontal className="h-3 w-3 text-muted-foreground/40" />
        </button>
      </div>

      {/* Folder Identity Row */}
      <div className="flex items-center gap-3">
        <div 
          className="shrink-0 h-9 w-9 rounded-xl flex items-center justify-center border-2 shadow-2xl transition-transform group-hover:scale-105 duration-300"
          style={{ 
            backgroundColor: `${folderColor}08`,
            borderColor: `${folderColor}20`,
            color: folderColor,
            boxShadow: `inset 0 0 12px ${folderColor}05`
          }}
        >
          {folder.icon ? (
            <span className="text-[14px] font-black drop-shadow-md">{folder.icon}</span>
          ) : (
            <FolderIcon className="h-4 w-4 drop-shadow-md" strokeWidth={2.5} />
          )}
        </div>
        <div className="flex flex-col gap-0.5 min-w-0">
           <h4 className="text-[12px] font-black leading-tight text-foreground/90 group-hover:text-foreground transition-colors truncate">
             {folder.name}
           </h4>
           <span className="text-[9px] font-bold text-muted-foreground/30 uppercase tracking-tight">
              Collection
           </span>
        </div>
      </div>
      
      {/* Footer Info / Progress */}
      <div className="mt-1 pt-2 border-t border-white/[0.02] flex items-center justify-between">
         <div className="flex items-center gap-1.5">
            <div className="h-1 w-20 bg-white/[0.02] rounded-full overflow-hidden border border-white/[0.03]">
               <div 
                  className="h-full rounded-full transition-all duration-1000"
                  style={{ backgroundColor: folderColor, width: "35%", opacity: 0.6 }} 
               />
            </div>
            <span className="text-[8px] font-black text-muted-foreground/20 uppercase tracking-widest">35%</span>
         </div>
         
         <div className="flex items-center gap-1 text-[8px] font-black text-muted-foreground/20 uppercase">
            <span>12</span>
            <span className="text-[6px] opacity-50">Tasks</span>
         </div>
      </div>

      {/* Selection Glow */}
      {isSelected && (
        <div className="absolute inset-0 rounded-xl bg-primary/[0.02] pointer-events-none" />
      )}
    </div>
  );
}

import { RichTextEditor } from "@/components/rich-text-editor";
import * as Icons from "lucide-react";

interface FolderOverviewMainProps {
  name: string;
  icon?: string;
  color?: string;
  description?: string;
  stats?: {
    totalTasks: number;
  };
  onDescriptionChange?: (val: string) => void;
}

export function FolderOverviewMain({
  name,
  icon,
  color,
  description,
  stats,
  onDescriptionChange,
}: FolderOverviewMainProps) {
  const IconComponent = (Icons as any)[icon || ""] || Icons.Folder;
  return (
    <div className="flex-1 flex flex-col p-8 overflow-y-auto no-scrollbar max-w-4xl mx-auto w-full bg-gradient-to-b from-background via-background/80 to-background/50">
      {/* Folder Title & Icon */}
      <div className="mb-10 flex items-center gap-6">
        <div className="shrink-0" style={{ color: color || "var(--primary)" }}>
          <IconComponent className="h-10 w-10 stroke-[1.2]" />
        </div>
        <div className="flex flex-col">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {name}
          </h1>
        </div>
      </div>

      {/* STATS SUMMARY */}
      <div className="flex items-center gap-16 mb-12">
        <div className="flex flex-col gap-1">
          <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/40">Total Tasks</span>
          <span className="text-3xl font-black text-foreground">{stats?.totalTasks || 0}</span>
        </div>
        <div className="flex flex-col gap-1 opacity-20">
          <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/40">Completion</span>
          <span className="text-3xl font-black text-foreground">--</span>
        </div>
      </div>

      {/* Description Section with Tiptap */}
      <div className="flex flex-col gap-4">
        <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30">
          Description
        </span>
        <RichTextEditor 
          value={description || ""}
          onChange={(val) => onDescriptionChange?.(val)}
          placeholder="Add folder scope or details..."
        />
      </div>
    </div>
  );
}

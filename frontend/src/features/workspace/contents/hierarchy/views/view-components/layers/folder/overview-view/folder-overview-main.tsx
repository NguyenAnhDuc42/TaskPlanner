import { RichTextEditor } from "@/components/rich-text-editor";
import * as Icons from "lucide-react";
import { ChevronRight } from "lucide-react";

interface FolderOverviewMainProps {
  name: string;
  icon?: string;
  color?: string;
  description?: string;
  entityInfo?: any;
  onDescriptionChange?: (val: string) => void;
}

export function FolderOverviewMain({
  name,
  icon,
  color,
  description,
  entityInfo,
  onDescriptionChange,
}: FolderOverviewMainProps) {
  const IconComponent = (Icons as any)[icon || ""] || Icons.Folder;

  return (
    <div className="flex-1 flex flex-col px-4 py-6 overflow-y-auto no-scrollbar max-w-4xl mx-auto w-full relative">
      {/* INTERNAL BREADCRUMBS */}
      <div className="flex items-center gap-1 text-muted-foreground/30 text-[9px] font-black uppercase tracking-[0.2em] mb-6 select-none">
        <span className="hover:text-muted-foreground/60 transition-colors cursor-pointer">
          {entityInfo?.parentName || "Projects"}
        </span>
        <ChevronRight className="h-2 w-2 opacity-30" />
        <span className="text-muted-foreground/60">{name}</span>
      </div>

      {/* Header Section */}
      <div className="mb-6 flex items-center gap-3">
        <div className="shrink-0 p-1.5 rounded-md bg-foreground/[0.03] border border-border/50" style={{ color: color || "var(--primary)" }}>
          <IconComponent className="h-5 w-5 stroke-[1.5]" />
        </div>
        <h1 className="text-xl font-black tracking-tight text-foreground">
          {name}
        </h1>
      </div>

      {/* Description Section */}
      <div className="flex flex-col gap-3">
        <div className="flex items-center gap-2 opacity-20">
          <Icons.FileText className="h-3 w-3" />
          <span className="text-[10px] font-black uppercase tracking-[0.2em]">
            Folder Scope
          </span>
        </div>
        <div className="min-h-[400px] p-5 rounded-xl border border-border/40 bg-background/50 shadow-sm">
          <RichTextEditor
            value={description || ""}
            onChange={(val) => onDescriptionChange?.(val)}
            placeholder="Document the scope and details for this folder..."
          />
        </div>
      </div>
    </div>
  );
}

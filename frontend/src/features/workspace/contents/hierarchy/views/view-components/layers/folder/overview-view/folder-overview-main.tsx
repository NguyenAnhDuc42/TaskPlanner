import { RichTextEditor } from "@/components/rich-text-editor";
import { Folder } from "lucide-react";

interface FolderOverviewMainProps {
  name: string;
  description?: string;
  onDescriptionChange?: (val: string) => void;
}

export function FolderOverviewMain({
  name,
  description,
  onDescriptionChange,
}: FolderOverviewMainProps) {
  return (
    <div className="flex-1 flex flex-col p-12 overflow-y-auto no-scrollbar max-w-4xl mx-auto w-full">
      {/* Folder Title & Icon */}
      <div className="mb-10 flex items-center gap-4">
        <div className="p-3 rounded-lg bg-muted/40 text-foreground shrink-0 border border-border/50 shadow-sm">
          <Folder className="h-6 w-6 stroke-[1.5]" />
        </div>
        <div className="flex flex-col">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {name}
          </h1>
        </div>
      </div>

      {/* Description Section with Tiptap */}
      <div className="space-y-4">
        <div className="text-[14px] font-medium text-foreground pb-2 border-b border-border/40">
          Folder Scope
        </div>

        <RichTextEditor 
          value={description || ""}
          onChange={(val) => onDescriptionChange?.(val)}
          placeholder="Add description..."
        />
      </div>
    </div>
  );
}

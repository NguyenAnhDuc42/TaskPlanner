import { cn } from "@/lib/utils";
import type { DocumentOutlineEntry } from "@/features/workspace/context/document-editor-context";

const ITEM_HEIGHT_PX = 24;
const MAX_HEIGHT_PX = 280;

export function DocumentOutlineRail({ outline, onNavigate }: Readonly<{ outline: DocumentOutlineEntry[]; onNavigate: (blockId: string) => void }>) {
  if (outline.length === 0) return null;
  const height = Math.min(outline.length * ITEM_HEIGHT_PX, MAX_HEIGHT_PX);

  return (
    <div className="group relative w-4" style={{ height }}>
      {outline.map((h, i) => (
        <span
          key={h.id}
          className="absolute right-0 h-0.5 w-3 rounded-full bg-muted-foreground/35 -translate-y-1/2 group-hover:bg-muted-foreground/60 transition-colors"
          style={{ top: `${outline.length === 1 ? 50 : (i / (outline.length - 1)) * 100}%` }}
        />
      ))}

      <div className="absolute right-full top-1/2 -translate-y-1/2 pr-2 hidden group-hover:block">
        <div className="w-56 max-h-72 overflow-y-auto rounded-lg border border-border/60 bg-popover shadow-xl p-1 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
          {outline.map((h) => (
            <button
              key={h.id}
              type="button"
              onClick={() => onNavigate(h.id)}
              className={cn(
                "w-full text-left text-[11px] px-2 py-1 rounded-md text-muted-foreground hover:bg-muted/60 hover:text-foreground truncate block cursor-pointer",
                h.level === 2 && "pl-4",
                h.level === 3 && "pl-6",
              )}
            >
              {h.text.trim() || "Untitled"}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

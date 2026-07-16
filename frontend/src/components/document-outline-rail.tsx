import { cn } from "@/lib/utils";
import type { DocumentOutlineEntry } from "@/features/workspace/context/document-editor-context";

const ITEM_HEIGHT_PX = 24;
const MAX_HEIGHT_PX = 280;

export function DocumentOutlineRail({ outline, onNavigate }: Readonly<{ outline: DocumentOutlineEntry[]; onNavigate: (blockId: string) => void }>) {
  if (outline.length === 0) return null;
  const height = Math.min(outline.length * ITEM_HEIGHT_PX, MAX_HEIGHT_PX);

  return (
    <div className="relative w-4" style={{ height }}>
      {outline.map((h, i) => (
        <button
          key={h.id}
          type="button"
          onClick={() => onNavigate(h.id)}
          title={h.text.trim() || "Untitled"}
          className="group/mark absolute -left-40 right-0 flex items-center justify-end h-3 -translate-y-1/2 cursor-pointer"
          style={{ top: `${outline.length === 1 ? 50 : (i / (outline.length - 1)) * 100}%` }}
        >
          <span className="max-w-0 opacity-0 overflow-hidden whitespace-nowrap text-[11px] leading-4 text-muted-foreground group-hover/mark:max-w-36 group-hover/mark:opacity-100 group-hover/mark:mr-1.5 transition-all duration-150">
            {h.text.trim() || "Untitled"}
          </span>
          <span
            className={cn(
              "rounded-full shrink-0 bg-muted-foreground/35 group-hover/mark:bg-muted-foreground/70 transition-colors",
              h.level === 1 ? "w-3 h-0.75" : "w-2 h-0.75",
            )}
          />
        </button>
      ))}
    </div>
  );
}

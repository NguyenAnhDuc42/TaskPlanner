import { DynamicIcon } from "@/components/dynamic-icon";
import type { DragItemData } from "./drag-item-type";

// Presentational clone for the DragOverlay. The real node component must NOT be rendered
// there: it mounts SortableItem — a second useSortable registered under the SAME id as the
// row being dragged — plus a context menu. The overlay only needs to look like the row;
// activeItem already carries the record's display fields.
export function DragOverlayRow({ item }: Readonly<{ item: DragItemData }>) {
  return (
    <div className="flex items-center px-1 py-0.5 rounded-md mb-px border bg-background text-muted-foreground border-transparent">
      <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-1.5">
        <DynamicIcon name={item.icon || "FileText"} size={14} color={item.color || "#ffffff"} />
      </div>
      <span className="text-[11px] leading-tight whitespace-nowrap font-semibold">
        {item.name}
      </span>
    </div>
  );
}

import { Circle } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { cn } from "@/lib/utils";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { DragItemData } from "./drag-item-type";

// Presentational clone for the DragOverlay. The real node components must NOT be rendered
// there: they mount SortableItem — a second useSortable registered under the SAME id as the
// row being dragged — and an expanded space additionally clones its entire folder/task
// subtree (each child another duplicate sortable) plus a context menu per row. The overlay
// only needs to look like the row; activeItem already carries the record's display fields.
export function DragOverlayRow({ item }: Readonly<{ item: DragItemData }>) {
  const isSpace = item.type === EntityLayerConst.ProjectSpace;
  const isTask = item.type === EntityLayerConst.ProjectTask;
  const fallbackIcon = isSpace ? "Orbit" : "Folder";

  return (
    <div className="flex items-center px-1 py-0.5 rounded-md mb-px border bg-background text-muted-foreground border-transparent">
      {isTask && <div className="w-1 h-1 rounded-full bg-muted-foreground/30 mr-1 shrink-0" />}
      <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-1.5">
        {isTask && !item.icon ? (
          <Circle className="h-3.5 w-3.5 text-white" />
        ) : (
          <DynamicIcon
            name={item.icon || fallbackIcon}
            size={14}
            color={item.color || "#ffffff"}
          />
        )}
      </div>
      <span className={cn("text-[11px] leading-tight whitespace-nowrap", isSpace ? "font-bold" : "font-semibold")}>
        {item.name}
      </span>
    </div>
  );
}

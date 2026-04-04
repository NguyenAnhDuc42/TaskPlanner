import type {
  TasksBoardViewResult,
  DisplayConfig,
  ViewDto,
} from "../../views-type";
import { Plus } from "lucide-react";
import { BoardColumn } from "./board-column";
import { type WheelEvent, useMemo, useRef, useState } from "react";

import type { EntityLayerType } from "@/types/relationship-type";
import { cn } from "@/lib/utils";

export function TaskBoardView({
  data,
  view,
  workspaceId,
  layerId,
  layerType,
  listId,
}: {
  data: TasksBoardViewResult;
  view: ViewDto;
  workspaceId: string;
  layerId: string;
  layerType: EntityLayerType;
  listId?: string;
}) {
  const [openInlineColumnId, setOpenInlineColumnId] = useState<string | null>(null);

  const boardScrollRef = useRef<HTMLDivElement | null>(null);

  const handleBoardWheel = (e: WheelEvent<HTMLDivElement>) => {
    const target = e.target as HTMLElement;
    if (target.closest("[data-radix-scroll-area-viewport]")) return;
    const container = boardScrollRef.current;
    if (!container) return;
    if (container.scrollWidth <= container.clientWidth) return;
    if (Math.abs(e.deltaY) <= Math.abs(e.deltaX)) return;
    container.scrollLeft += e.deltaY;
    e.preventDefault();
  };

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : { groupBy: "status" };

  const isGroupedByStatus = displayConfig.groupBy === "status";

 


  return (
    <div
      ref={boardScrollRef}
      onWheel={handleBoardWheel}
      className="h-full flex gap-16 overflow-x-auto pb-12 no-scrollbar items-start relative px-2"
    ></div>
  );
}

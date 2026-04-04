import type { TaskListViewResult, ViewDto } from "../../views-type";
import { StatusSection } from "./status-section";

import { useMemo, useState } from "react";

import { EntityLayerType } from "@/types/relationship-type";
import { cn } from "@/lib/utils";

export function TaskListView({
  data,
  view,
  workspaceId,
  layerId,
  layerType,
  listId,
}: {
  data: TaskListViewResult;
  view: ViewDto;
  workspaceId: string;
  layerId: string;
  layerType: EntityLayerType;
  listId?: string;
}) {
  const [openInlineStatusId, setOpenInlineStatusId] = useState<string | null>(null);

  const displayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : { groupBy: "status", visibleColumns: ["assignee", "dueDate", "priority"] };

  const isGroupedByStatus = displayConfig.groupBy === "status";
  const visibleCols = displayConfig.visibleColumns || ["assignee", "dueDate", "priority"];

  return (
    <div className="space-y-8 pb-20 relative">

    </div>
  );
}

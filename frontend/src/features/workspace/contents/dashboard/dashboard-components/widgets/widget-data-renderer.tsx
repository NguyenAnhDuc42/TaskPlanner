import { useEffect } from "react";
import { Loader2 } from "lucide-react";
import { WidgetType } from "@/types/widget-type";
import type { WidgetData, WidgetDto } from "../../dashboard-type";
import { TaskListWidget } from "./task-list-widget";

interface WidgetDataRendererProps {
  widget: WidgetDto;
  data?: WidgetData;
}

/**
 * A polymorphic renderer that maps widget types to their specialized components.
 * Handles the "Syncing" state until the background SignalR data arrives.
 */
export function WidgetDataRenderer({ widget, data }: WidgetDataRendererProps) {
  useEffect(() => {
    if (data) {
      console.log(`[WidgetDataRenderer] ${widget.id} (${widget.widgetType}) has data:`, data);
    }
  }, [widget.id, widget.widgetType, data]);

  // 1. If we have data, render the specialized widget
  if (data) {
    switch (widget.widgetType) {
      case WidgetType.TaskList:
        // Ensure the data matches the expected type for TaskList
        if (data.type === WidgetType.TaskList) {
          return <TaskListWidget data={data} />;
        }
        break;
      
      case WidgetType.FolderList:
        return (
          <div className="h-full flex flex-col items-center justify-center p-4 text-muted-foreground/60 text-xs gap-2">
            <span className="font-semibold uppercase tracking-wider text-[10px]">Folder List</span>
            <span className="italic">No folders found.</span>
          </div>
        );

      case WidgetType.ActivityFeed:
        return (
          <div className="h-full flex flex-col items-center justify-center p-4 text-muted-foreground/60 text-xs gap-2">
            <span className="font-semibold uppercase tracking-wider text-[10px]">Activity Feed</span>
            <span className="italic">No recent activity.</span>
          </div>
        );
      
      // Future widgets will be added here:
    }
  }

  // 2. Default/Loading State (while waiting for SignalR push)
  return (
    <div className="h-full flex flex-col items-center justify-center p-4 bg-muted/[0.03]">
      <Loader2 className="h-5 w-5 animate-spin text-muted-foreground/40 mb-2" />
      <div className="flex flex-col items-center gap-0.5">
        <span className="text-[10px] uppercase font-bold text-muted-foreground/30 tracking-tight italic">
          Syncing Dynamic Data
        </span>
        <span className="text-[8px] uppercase font-medium text-muted-foreground/20">
          Background build in progress
        </span>
      </div>
    </div>
  );
}

import { WidgetType } from "@/types/widget-type";
import type { WidgetData, WidgetDto } from "../../dashboard-type";
import { TaskListWidget } from "./task-list-widget";
import { WidgetSkeleton } from "./widget-skeleton";

interface WidgetDataRendererProps {
  widget: WidgetDto;
  data?: WidgetData;
}

/**
 * A polymorphic renderer that maps widget types to their specialized components.
 * Handles the "Syncing" state until the background SignalR data arrives.
 */
export function WidgetDataRenderer({ widget, data }: WidgetDataRendererProps) {
  // 1. If we have data, render the specialized widget
  if (data) {
    switch (widget.widgetType) {
      case WidgetType.TaskList:
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
    }
  }

  // 2. Loading State — skeleton that mimics the widget shape
  return <WidgetSkeleton />;
}


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
          <div className="h-full flex flex-col items-center justify-center p-4 text-[var(--theme-text-normal)] text-xs gap-3 opacity-60">
            <span className="font-bold uppercase tracking-[0.2em] text-[9px]">Folder List</span>
            <span className="italic text-[10px]">No projects or folders found.</span>
          </div>
        );

      case WidgetType.ActivityFeed:
        return (
          <div className="h-full flex flex-col items-center justify-center p-4 text-[var(--theme-text-normal)] text-xs gap-3 opacity-60">
            <span className="font-bold uppercase tracking-[0.2em] text-[9px]">Activity Feed</span>
            <span className="italic text-[10px]">No recent activity in this section.</span>
          </div>
        );
    }
  }

  // 2. Loading State — skeleton that mimics the widget shape
  return <WidgetSkeleton />;
}


import { Dialog, DialogContent } from "@/components/ui/dialog";
import { WidgetType } from "@/types/widget-type";
import type { WidgetData, WidgetDto } from "../../dashboard-type";
import { TaskListWidget } from "./task-list-widget";

interface WidgetDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  widget: WidgetDto | null;
  data?: WidgetData;
}

/**
 * Card-styled dialog for viewing a widget's complete data.
 * Looks like a proper window with a header bar, visible borders, and a close button.
 */
export function WidgetDetailDialog({ open, onOpenChange, widget, data }: WidgetDetailDialogProps) {
  if (!widget) return null;

  const renderContent = () => {
    if (!data) {
      return (
        <div className="flex items-center justify-center h-40 text-muted-foreground text-sm">
          No data available yet. Waiting for sync...
        </div>
      );
    }

    switch (widget.widgetType) {
      case WidgetType.TaskList:
        if (data.type === WidgetType.TaskList) {
          return <TaskListWidget data={data} expanded />;
        }
        break;
    }

    return (
      <div className="flex items-center justify-center h-40 text-muted-foreground text-sm">
        Widget type not supported for expanded view yet.
      </div>
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl p-0 overflow-hidden border rounded-lg shadow-xl gap-0">
        {/* Window-style header bar */}
        <div className="flex items-center justify-between px-4 py-2.5 border-b bg-muted/30">
          <span className="text-[11px] font-bold uppercase tracking-widest text-muted-foreground">
            {widget.widgetType}
          </span>
          
        </div>

        {/* Scrollable content area */}
        <div className="max-h-[65vh] overflow-y-auto">
          {renderContent()}
        </div>
      </DialogContent>
    </Dialog>
  );
}

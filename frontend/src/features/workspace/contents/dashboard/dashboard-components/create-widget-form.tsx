import { useState } from "react";
import { useCreateWidget } from "../dashboard-api";
import { WidgetType } from "@/types/widget-type";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { 
  ListTodo, 
  Rss, 
  Bell, 
  Clock, 
  ShieldCheck, 
  BarChart3, 
  Target, 
  Zap, 
  Calendar as CalendarIcon, 
  LayoutTemplate,
  FolderTree
} from "lucide-react";

const WIDGET_OPTIONS = [
  {
    type: WidgetType.TaskList,
    label: "Task List",
    description: "Track tasks and their status.",
    icon: ListTodo,
    color: "text-blue-500",
    bg: "bg-blue-500/10",
  },
  {
    type: WidgetType.FolderList,
    label: "Folder List",
    description: "Show projects and folders.",
    icon: FolderTree,
    color: "text-purple-500",
    bg: "bg-purple-500/10",
  },
  {
    type: WidgetType.ActivityFeed,
    label: "Activity Feed",
    description: "Latest updates in workspace.",
    icon: Rss,
    color: "text-orange-500",
    bg: "bg-orange-500/10",
  },
  {
    type: WidgetType.UpcomingDeadlines,
    label: "Deadlines",
    description: "Keep track of due dates.",
    icon: Clock,
    color: "text-red-500",
    bg: "bg-red-500/10",
  },
  {
    type: WidgetType.QuickActions,
    label: "Quick Actions",
    description: "Fast shortcuts for common tasks.",
    icon: Zap,
    color: "text-amber-500",
    bg: "bg-amber-500/10",
  },
  {
    type: WidgetType.Calendar,
    label: "Calendar",
    description: "Visual schedule of your work.",
    icon: CalendarIcon,
    color: "text-indigo-500",
    bg: "bg-indigo-500/10",
  },
];

export function CreateWidgetForm({ 
  dashboardId, 
  onSuccess 
}: { 
  dashboardId: string; 
  onSuccess: () => void 
}) {
  const [selectedType, setSelectedType] = useState<WidgetType>(WidgetType.TaskList);
  const createWidget = useCreateWidget();

  const handleSubmit = async () => {
    if (!dashboardId) return;

    await createWidget.mutateAsync({
      dashboardId,
      widgetType: selectedType,
      Col: 0,
      Row: 0,
      Width: 4,
      Height: 4,
    });
    onSuccess();
  };

  return (
    <div className="space-y-6 py-4">
      <div className="grid grid-cols-1 gap-3">
        {WIDGET_OPTIONS.map((option) => (
          <button
            key={option.type}
            type="button"
            onClick={() => setSelectedType(option.type as WidgetType)}
            className={cn(
              "flex items-start gap-4 p-4 rounded-xl border transition-all text-left group",
              selectedType === option.type 
                ? "border-primary bg-primary/[0.02] ring-1 ring-primary/20" 
                : "border-border hover:border-primary/40 hover:bg-muted/50"
            )}
          >
            <div className={cn("p-2.5 rounded-lg shrink-0 transition-colors", option.bg, option.color)}>
              <option.icon className="h-5 w-5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="font-semibold text-sm">{option.label}</div>
              <div className="text-xs text-muted-foreground mt-0.5 line-clamp-2">
                {option.description}
              </div>
            </div>
            <div className={cn(
              "h-2 w-2 rounded-full mt-1.5 shrink-0 transition-opacity",
              selectedType === option.type ? "bg-primary opacity-100" : "bg-muted opacity-0 group-hover:opacity-50"
            )} />
          </button>
        ))}
      </div>
      
      <div className="flex justify-end pt-2">
        <Button 
          onClick={handleSubmit} 
          className="w-full sm:w-auto px-8" 
          disabled={createWidget.isPending}
        >
          {createWidget.isPending ? "Creating..." : "Create Widget"}
        </Button>
      </div>
    </div>
  );
}

import { useState } from "react";
import { useCreateWidget } from "../dashboard-api";
import { WidgetType } from "@/types/widget-type";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { 
  ListTodo, 
  Rss, 
  Clock, 
  Zap, 
  Calendar as CalendarIcon, 
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
      <div className="grid grid-cols-1 gap-2.5">
        {WIDGET_OPTIONS.map((option) => (
          <button
            key={option.type}
            type="button"
            onClick={() => setSelectedType(option.type as WidgetType)}
            className={cn(
              "flex items-start gap-4 p-4 rounded-xl border transition-all text-left group box-border",
              selectedType === option.type 
                ? "border-[var(--theme-bg-glow)] bg-[var(--theme-item-hover)] shadow-sm" 
                : "border-border/5 hover:border-border/10 hover:bg-[var(--theme-item-normal)]"
            )}
          >
            <div className={cn("p-2.5 rounded-lg shrink-0 transition-colors bg-transparent border border-current opacity-60 group-hover:opacity-100", option.color)}>
              <option.icon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="font-bold text-[11px] uppercase tracking-wider text-[var(--theme-text-hover)]">{option.label}</div>
              <div className="text-[10px] text-[var(--theme-text-normal)] mt-0.5 line-clamp-1 opacity-60 group-hover:opacity-100 transition-opacity">
                {option.description}
              </div>
            </div>
            <div className={cn(
              "h-1.5 w-1.5 rounded-full mt-2 shrink-0 transition-all",
              selectedType === option.type ? "bg-[var(--theme-bg-glow)] scale-125" : "bg-white/10 opacity-0 group-hover:opacity-50"
            )} />
          </button>
        ))}
      </div>
      
      <div className="flex justify-end pt-2">
        <Button 
          onClick={handleSubmit} 
          className="w-full theme-selected border-0 transition-all hover:scale-[1.02]" 
          disabled={createWidget.isPending}
        >
          {createWidget.isPending ? "Creating..." : "Create Widget"}
        </Button>
      </div>
    </div>
  );
}

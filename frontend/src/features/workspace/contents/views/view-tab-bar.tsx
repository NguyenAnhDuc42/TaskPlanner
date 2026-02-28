import type { ViewDto } from "./views-type";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Plus, Settings2, List, LayoutDashboard, FileText } from "lucide-react";
import { useCreateView } from "./views-api";
import { ViewType } from "@/types/view-type";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface ViewTabBarProps {
  views: ViewDto[];
  activeViewId: string | null;
  onViewChange: (view: ViewDto) => void;
  layerId: string;
  layerType: string;
}

export function ViewTabBar({
  views,
  activeViewId,
  onViewChange,
  layerId,
  layerType,
}: ViewTabBarProps) {
  const createView = useCreateView();

  const handleAddView = (type: ViewType, defaultName: string) => {
    createView.mutate({
      layerId,
      layerType,
      name: defaultName,
      viewType: type,
    });
  };

  return (
    <div className="flex items-center border-b px-4 h-12 gap-1 bg-background flex-shrink-0 overflow-x-auto no-scrollbar">
      {views.map((view) => (
        <button
          key={view.id}
          onClick={() => onViewChange(view)}
          className={cn(
            "px-4 h-full text-sm font-medium transition-all relative border-b-2 border-transparent",
            activeViewId === view.id
              ? "text-primary border-primary bg-primary/5"
              : "text-muted-foreground hover:text-foreground hover:bg-accent/50",
          )}
        >
          {view.name}
        </button>
      ))}

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="icon" className="h-8 w-8 ml-2">
            <Plus className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start">
          <DropdownMenuItem
            onClick={() => handleAddView(ViewType.List, "New List")}
          >
            <List className="h-4 w-4 mr-2 text-muted-foreground" />
            List
          </DropdownMenuItem>
          <DropdownMenuItem
            onClick={() => handleAddView(ViewType.Board, "New Board")}
          >
            <LayoutDashboard className="h-4 w-4 mr-2 text-muted-foreground" />
            Board
          </DropdownMenuItem>
          <DropdownMenuItem
            onClick={() => handleAddView(ViewType.Doc, "New Doc")}
          >
            <FileText className="h-4 w-4 mr-2 text-muted-foreground" />
            Doc
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-2">
        <Button
          variant="ghost"
          size="sm"
          className="h-8 gap-2 text-muted-foreground"
        >
          <Settings2 className="h-3.5 w-3.5" />
          <span className="text-xs">Customize</span>
        </Button>
      </div>
    </div>
  );
}

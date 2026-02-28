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
    <div className="flex items-center border-b px-6 h-11 gap-1.5 bg-background/60 backdrop-blur-xl flex-shrink-0 overflow-x-auto no-scrollbar relative z-20">
      {views.map((view) => (
        <button
          key={view.id}
          onClick={() => onViewChange(view)}
          className={cn(
            "px-4 h-8 rounded-lg text-[13px] font-semibold transition-all duration-200 relative whitespace-nowrap",
            activeViewId === view.id
              ? "text-primary bg-primary/10 shadow-[0_2px_10px_-3px_rgba(59,130,246,0.3)]"
              : "text-muted-foreground hover:text-foreground hover:bg-muted/50",
          )}
        >
          {view.name}
        </button>
      ))}

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 ml-2 rounded-full hover:bg-primary/5 hover:text-primary transition-colors"
          >
            <Plus className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-48 p-1.5 rounded-xl border-muted-foreground/10 bg-card/90 backdrop-blur-lg"
        >
          <DropdownMenuItem
            className="rounded-lg gap-2 cursor-pointer"
            onClick={() => handleAddView(ViewType.List, "New List")}
          >
            <List className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">List View</span>
          </DropdownMenuItem>
          <DropdownMenuItem
            className="rounded-lg gap-2 cursor-pointer"
            onClick={() => handleAddView(ViewType.Board, "New Board")}
          >
            <LayoutDashboard className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">Board View</span>
          </DropdownMenuItem>
          <DropdownMenuItem
            className="rounded-lg gap-2 cursor-pointer"
            onClick={() => handleAddView(ViewType.Doc, "New Doc")}
          >
            <FileText className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">Document</span>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-3">
        <div className="h-4 w-px bg-muted-foreground/20 mx-1" />
        <Button
          variant="ghost"
          size="sm"
          className="h-8 gap-2 text-muted-foreground hover:text-foreground px-3 rounded-lg font-bold"
        >
          <Settings2 className="h-3.5 w-3.5" />
          <span className="text-[12px] uppercase tracking-wider">
            Customize
          </span>
        </Button>
      </div>
    </div>
  );
}

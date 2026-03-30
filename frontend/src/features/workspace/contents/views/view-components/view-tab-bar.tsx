import type { ViewDto } from "../views-type";
import { cn } from "@/lib/utils";
import { Plus, Settings2, List, LayoutDashboard, FileText } from "lucide-react";
import { useCreateView } from "../views-api";
import { ViewType } from "@/types/view-type";
import { EntityLayerType } from "@/types/relationship-type";
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
  layerType: EntityLayerType;
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
    <div className="flex items-center px-4 h-[44px] gap-1 bg-background/20 backdrop-blur-md flex-shrink-0 overflow-x-auto no-scrollbar relative z-20 border-b border-white/5">
      {views.map((view) => {
        const isActive = activeViewId === view.id;
        return (
          <div
            key={view.id}
            onClick={() => onViewChange(view)}
            className={cn(
              "group relative h-full flex items-center px-4 cursor-pointer transition-all duration-300",
              isActive ? "opacity-100" : "opacity-40 hover:opacity-100",
            )}
          >
            <span
              className={cn(
                "text-[10px] font-black uppercase tracking-[0.2em] whitespace-nowrap",
                isActive ? "text-foreground" : "text-muted-foreground",
              )}
            >
              {view.name}
            </span>
            {isActive && (
              <div className="absolute bottom-0 left-4 right-4 h-0.5 bg-primary rounded-full shadow-[0_0_8px_var(--primary)]" />
            )}
          </div>
        );
      })}

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <div className="flex items-center justify-center h-8 w-8 ml-2 rounded-full hover:bg-white/10 cursor-pointer transition-colors group">
            <Plus className="h-3.5 w-3.5 text-muted-foreground/60 group-hover:text-primary transition-colors" />
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          className="w-52 p-1.5 rounded-xl border-white/10 bg-card/40 backdrop-blur-2xl shadow-2xl"
        >
          <div className="px-3 py-2 text-[9px] font-black text-muted-foreground/40 uppercase tracking-[0.3em]">
            Observation Types
          </div>
          <DropdownMenuItem
            className="rounded-lg gap-3 cursor-pointer py-2.5 active:scale-95 transition-transform"
            onClick={() => handleAddView(ViewType.List, "New List")}
          >
            <List className="h-4 w-4 text-muted-foreground/60" />
            <span className="text-xs font-bold uppercase tracking-widest text-muted-foreground/80">List Array</span>
          </DropdownMenuItem>
          <DropdownMenuItem
            className="rounded-lg gap-3 cursor-pointer py-2.5 active:scale-95 transition-transform"
            onClick={() => handleAddView(ViewType.Board, "New Board")}
          >
            <LayoutDashboard className="h-4 w-4 text-muted-foreground/60" />
            <span className="text-xs font-bold uppercase tracking-widest text-muted-foreground/80">Spatial Board</span>
          </DropdownMenuItem>
          <DropdownMenuItem
            className="rounded-lg gap-3 cursor-pointer py-2.5 active:scale-95 transition-transform"
            onClick={() => handleAddView(ViewType.Doc, "New Doc")}
          >
            <FileText className="h-4 w-4 text-muted-foreground/60" />
            <span className="text-xs font-bold uppercase tracking-widest text-muted-foreground/80">Intelligence Doc</span>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="ml-auto flex items-center gap-4">
        <div className="h-4 w-px bg-white/5" />
        <div className="flex items-center gap-2 group cursor-pointer">
          <Settings2 className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-foreground transition-colors" />
          <span className="text-[10px] uppercase tracking-[0.2em] font-black text-muted-foreground/40 group-hover:text-foreground transition-colors">
            Configure
          </span>
        </div>
      </div>
    </div>
  );
}

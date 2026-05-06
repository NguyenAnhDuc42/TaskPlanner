import {
  ChevronRight,
  Info,
  MoreHorizontal,
  LayoutGrid,
  List,
  Filter,
  Check,
  Paperclip,
  Folder,
} from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Button } from "@/components/ui/button";
import type { MainViewTab, ItemsViewMode } from "../layer-detail-types";
import type { RightPanelType } from "../layer-view";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

interface LayerHeaderProps {
  viewData: any;
  activeTab: MainViewTab;
  viewMode: ItemsViewMode;
  onViewModeChange: (mode: ItemsViewMode) => void;
  rightPanelType: RightPanelType;
  onToggleRightPanel: (type: RightPanelType) => void;
}

export function LayerHeader({
  viewData,
  activeTab,
  viewMode,
  onViewModeChange,
  rightPanelType,
  onToggleRightPanel,
}: LayerHeaderProps) {
  if (!viewData) return null;

  return (
    <div className="flex items-center justify-between px-4 h-8 bg-background/80 backdrop-blur-md border-b border-border/40 flex-shrink-0 select-none">
      {/* --- Left: Breadcrumbs --- */}
      <div className="flex items-center gap-0.5 text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest h-full">
        <span className="hover:text-foreground transition-colors cursor-pointer">
          Workspace
        </span>
        <ChevronRight className="h-2.5 w-2.5 opacity-40 mx-0.5" />
        <div className="flex items-center gap-1 text-foreground/70 h-full">
          <DynamicIcon
            name={viewData.icon || "Folder"}
            size={12}
            color={viewData.color}
            className="stroke-[2.5]"
          />
          <span className="truncate max-w-[200px] font-black">
            {viewData.name}
          </span>
        </div>
      </div>

      {/* --- Right: Global Actions --- */}
      <div className="flex items-center gap-0.5">
        {/* View Mode Dropdown (Only in Items tab) */}
        {activeTab === "items" && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-foreground transition-colors"
              >
                <Filter className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className="w-56 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl"
            >
              <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">
                  Display Mode
                </span>
              </div>
              <DropdownMenuItem
                onClick={() => onViewModeChange("board")}
                className="flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer"
              >
                <div className="flex items-center gap-2">
                  <LayoutGrid className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-bold uppercase tracking-wider">
                    Board View
                  </span>
                </div>
                {viewMode === "board" && (
                  <Check className="h-3 w-3 text-primary" />
                )}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => onViewModeChange("list")}
                className="flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer"
              >
                <div className="flex items-center gap-2">
                  <List className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-bold uppercase tracking-wider">
                    List View
                  </span>
                </div>
                {viewMode === "list" && (
                  <Check className="h-3 w-3 text-primary" />
                )}
              </DropdownMenuItem>
              <DropdownMenuSeparator className="bg-border/10 my-1" />
              <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">
                  Filters
                </span>
              </div>
              <FilterItem label="Everything" active />
              <FilterItem label="Assigned to me" />
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        <div className="h-4 w-px bg-border/40 mx-1" />

        {/* New Trigger Buttons for Right Panel */}
        <Button
          variant="ghost"
          size="icon"
          className={cn(
            "h-7 w-7 text-muted-foreground hover:text-foreground transition-all rounded-md",
            rightPanelType === "attachments" && "bg-primary/10 text-primary",
          )}
          onClick={() => onToggleRightPanel("attachments")}
        >
          <Paperclip className="h-3.5 w-3.5" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className={cn(
            "h-7 w-7 text-muted-foreground hover:text-foreground transition-all rounded-md",
            rightPanelType === "properties" && "bg-primary/10 text-primary",
          )}
          onClick={() => onToggleRightPanel("properties")}
        >
          <Info className="h-3.5 w-3.5" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 rounded-md text-muted-foreground hover:text-foreground"
        >
          <MoreHorizontal className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}

function FilterItem({ label, active }: { label: string; active?: boolean }) {
  return (
    <DropdownMenuItem
      className={cn(
        "flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer transition-colors",
        active ? "bg-primary/5 text-primary" : "hover:bg-foreground/[0.03]",
      )}
    >
      <span className="text-[11px] font-bold">{label}</span>
      {active && <Check className="h-3 w-3" />}
    </DropdownMenuItem>
  );
}

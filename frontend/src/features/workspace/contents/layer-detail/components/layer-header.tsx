import { 
  ChevronRight, 
  Info, 
  MoreHorizontal, 
  LayoutGrid, 
  List, 
  Filter,
  Check,
  Tag,
  Shield,
  Calendar,
  Clock,
  Folder,
  Layout
} from "lucide-react";
import * as Icons from "lucide-react";
import { Button } from "@/components/ui/button";
import type { MainViewTab, ItemsViewMode } from "../layer-detail-types";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

interface LayerHeaderProps {
  entityInfo: any;
  activeTab: MainViewTab;
  viewMode: ItemsViewMode;
  onViewModeChange: (mode: ItemsViewMode) => void;
}

export function LayerHeader({
  entityInfo,
  activeTab,
  viewMode,
  onViewModeChange,
}: LayerHeaderProps) {
  if (!entityInfo) return null;

  const IconComponent = (Icons as any)[entityInfo.icon] || Folder;

  return (
    <div className="flex items-center justify-between px-4 h-8 bg-background/80 backdrop-blur-md border-b border-border/40 flex-shrink-0 select-none">
      
      {/* --- Left: Breadcrumbs --- */}
      <div className="flex items-center gap-1.5 text-[10px] font-bold text-muted-foreground/30 uppercase tracking-widest">
        <span className="hover:text-muted-foreground/60 transition-colors cursor-pointer">Workspace</span>
        <ChevronRight className="h-2.5 w-2.5 opacity-20" />
        <div className="flex items-center gap-2 text-muted-foreground/60">
           <IconComponent className="h-3 w-3 stroke-[2.5]" style={{ color: entityInfo.color }} />
           <span className="truncate max-w-[200px] font-black">{entityInfo.name}</span>
        </div>
      </div>

      {/* --- Right: Unified Action Dropdown --- */}
      <div className="flex items-center gap-1">
        {activeTab === "items" ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-foreground transition-colors">
                <Filter className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
              {/* View Mode Section */}
              <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Display Mode</span>
              </div>
              <DropdownMenuItem onClick={() => onViewModeChange("board")} className="flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer">
                <div className="flex items-center gap-2">
                  <LayoutGrid className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-bold uppercase tracking-wider">Board View</span>
                </div>
                {viewMode === "board" && <Check className="h-3 w-3 text-primary" />}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onViewModeChange("list")} className="flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer">
                <div className="flex items-center gap-2">
                  <List className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-bold uppercase tracking-wider">List View</span>
                </div>
                {viewMode === "list" && <Check className="h-3 w-3 text-primary" />}
              </DropdownMenuItem>

              <DropdownMenuSeparator className="bg-border/10 my-1" />

              {/* Filters Section */}
              <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Filters</span>
              </div>
              <FilterItem label="Everything" active />
              <FilterItem label="Assigned to me" />
              <DropdownMenuSeparator className="bg-border/10" />
              <FilterItem label="Hide completed" />
            </DropdownMenuContent>
          </DropdownMenu>
        ) : (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-foreground">
                <Layout className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-80 p-0 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl overflow-hidden">
               <div className="p-4 border-b border-border/10 bg-muted/5">
                <span className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/40">Node Properties</span>
              </div>
              <div className="p-2 space-y-0.5">
                <DropdownPropRow icon={Tag} label="Identifier" value={entityInfo.id.slice(0, 8)} />
                <DropdownPropRow icon={Shield} label="Access" value="Public Node" />
                <DropdownPropRow icon={Calendar} label="Deployed" value="May 02, 2026" />
                <DropdownPropRow icon={Clock} label="Last Activity" value="2m ago" />
              </div>
            </DropdownMenuContent>
          </DropdownMenu>
        )}

        <div className="h-4 w-px bg-border/40 mx-1" />
        <Button variant="ghost" size="icon" className="h-7 w-7 rounded-md">
          <MoreHorizontal className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}

function FilterItem({ label, active }: { label: string; active?: boolean }) {
  return (
    <DropdownMenuItem className={cn(
      "flex items-center justify-between px-2 py-1.5 rounded-lg cursor-pointer transition-colors",
      active ? "bg-primary/5 text-primary" : "hover:bg-foreground/[0.03]"
    )}>
      <span className="text-[11px] font-bold">{label}</span>
      {active && <Check className="h-3 w-3" />}
    </DropdownMenuItem>
  );
}

function DropdownPropRow({ icon: Icon, label, value }: { icon: any, label: string, value: string }) {
  return (
    <div className="flex items-center justify-between px-3 py-2 hover:bg-muted/30 rounded-lg transition-colors group">
      <div className="flex items-center gap-2.5">
        <Icon className="h-3.5 w-3.5 text-muted-foreground/30 group-hover:text-primary transition-colors" />
        <span className="text-[11px] font-semibold text-muted-foreground/60">{label}</span>
      </div>
      <span className="text-[11px] font-black text-foreground/80 tracking-tight">{value}</span>
    </div>
  );
}

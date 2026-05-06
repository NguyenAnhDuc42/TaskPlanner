import { cn } from "@/lib/utils";
import { EntityLayerType } from "@/types/entity-layer-type";
import type { MainViewTab } from "../layer-detail-types";

interface LayerTabsProps {
  activeTab: MainViewTab;
  onTabChange: (tab: MainViewTab) => void;
  layerType: EntityLayerType;
}

export function LayerTabs({ activeTab, onTabChange, layerType }: LayerTabsProps) {
  const isTask = layerType === EntityLayerType.ProjectTask;

  return (
    <div className="flex items-center gap-1 px-4 h-8 bg-background/50 border-b border-border/40 select-none">
      <TabButton 
        active={activeTab === "overview"} 
        onClick={() => onTabChange("overview")} 
        label="Overview" 
      />
      {!isTask && (
        <TabButton 
          active={activeTab === "items"} 
          onClick={() => onTabChange("items")} 
          label="Items" 
        />
      )}
    </div>
  );
}

function TabButton({ active, onClick, label }: { active: boolean, onClick: () => void, label: string }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "px-2 h-full flex items-center text-[10px] font-black uppercase tracking-[0.1em] transition-all relative group",
        active 
          ? "text-primary" 
          : "text-muted-foreground/40 hover:text-muted-foreground/60"
      )}
    >
      {label}
      {active && (
        <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-primary rounded-t-full shadow-[0_-2px_8px_rgba(var(--primary-rgb),0.5)]" />
      )}
    </button>
  );
}

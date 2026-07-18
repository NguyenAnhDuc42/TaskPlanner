import type { ReactNode } from "react";
import { Settings } from "lucide-react";
import { cn } from "@/lib/utils";
import { SPACE_RAIL_TABS, type SpaceRailTabKey } from "./space-rail-tabs";

interface SpaceViewRailProps {
  tabOrder: SpaceRailTabKey[];
  activeTab: SpaceRailTabKey;
  onTabChange: (tab: SpaceRailTabKey) => void;
  onOpenSettings: () => void;
  orientation?: "side" | "top";
  leading?: ReactNode;
  trailing?: ReactNode;
}

export function SpaceViewRail({ tabOrder, activeTab, onTabChange, onOpenSettings, orientation = "side", leading, trailing }: Readonly<SpaceViewRailProps>) {
  if (orientation === "top") {
    return (
      <div className="flex flex-col shrink-0">
        <div className="flex items-center gap-1 h-8 px-2 border-b border-border/40 bg-card">
          {leading}
          <div className="ml-auto flex items-center">{trailing}</div>
        </div>
        <div className="flex items-center gap-1 h-8 px-2 border-b border-border bg-card">
          {tabOrder.map((key) => {
            const tab = SPACE_RAIL_TABS[key];
            const Icon = tab.icon;
            return (
              <button
                key={key}
                type="button"
                onClick={() => onTabChange(key)}
                title={tab.label}
                className={cn(
                  "flex items-center gap-1.5 h-7 px-2 rounded-md transition-colors cursor-pointer",
                  activeTab === key ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                )}
              >
                <Icon className="h-3.5 w-3.5 shrink-0" />
                <span className="text-[10px] font-semibold whitespace-nowrap">{tab.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    );
  }

  return (
    <div className="relative w-9 shrink-0">
      <div className="group/rail absolute inset-y-0 left-0 z-20 flex flex-col items-stretch gap-1 w-9 hover:w-32 py-2 border-r border-border bg-card overflow-hidden transition-[width] duration-150 ease-out">
        <div className="flex flex-col items-stretch gap-1">
          {tabOrder.map((key) => {
            const tab = SPACE_RAIL_TABS[key];
            const Icon = tab.icon;
            return (
              <button
                key={key}
                type="button"
                onClick={() => onTabChange(key)}
                title={tab.label}
                className={cn(
                  "flex items-center gap-2 h-7 mx-1 px-1.5 rounded-md transition-colors cursor-pointer",
                  activeTab === key ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                )}
              >
                <Icon className="h-3.5 w-3.5 shrink-0" />
                <span className="text-[10px] font-semibold whitespace-nowrap opacity-0 group-hover/rail:opacity-100 transition-opacity duration-150">
                  {tab.label}
                </span>
              </button>
            );
          })}
        </div>

        <button
          type="button"
          onClick={onOpenSettings}
          title="Space Settings"
          className="mt-auto flex items-center gap-2 h-7 mx-1 px-1.5 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer"
        >
          <Settings className="h-3.5 w-3.5 shrink-0" />
          <span className="text-[10px] font-semibold whitespace-nowrap opacity-0 group-hover/rail:opacity-100 transition-opacity duration-150">
            Settings
          </span>
        </button>
      </div>
    </div>
  );
}

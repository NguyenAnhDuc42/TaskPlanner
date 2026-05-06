import { useState } from "react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { LayerHeader } from "./components/layer-header";
import { LayerTabs } from "./components/layer-tabs";
import { OverviewView } from "./views/overview-view";
import { ItemsView } from "./views/items-view";
import { PropertySidebar } from "./components/overview/property-sidebar";
import { AttachmentSection } from "./components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "./layer-detail-types";
import { cn } from "@/lib/utils";

interface LayerViewProps {
  layerType: EntityLayerType;
  viewData: any;
  isLoading: boolean;
  workspaceId?: string; // Optional if passed but unused
  entityId?: string; // Optional if passed but unused
  views?: any[]; // Optional if passed but unused
}

export type RightPanelType = "properties" | "attachments" | null;

export function LayerView({ viewData, isLoading, layerType }: LayerViewProps) {
  const [activeTab, setActiveTab] = useState<MainViewTab>("overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("board");
  const [rightPanelType, setRightPanelType] =
    useState<RightPanelType>("properties");

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <LayerHeader
        viewData={viewData}
        activeTab={activeTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        rightPanelType={rightPanelType}
        onToggleRightPanel={toggleRightPanel}
      />

      <LayerTabs
        activeTab={activeTab}
        onTabChange={setActiveTab}
        layerType={layerType}
      />

      <div className="flex-1 flex overflow-hidden relative">
        <div className="flex-1 overflow-hidden relative">
          {activeTab === "overview" && (
            <OverviewView viewData={viewData} layerType={layerType} />
          )}
          {activeTab === "items" && (
            <ItemsView
              viewData={viewData}
              isLoading={isLoading}
              layerType={layerType}
              viewMode={viewMode}
            />
          )}
        </div>

        <div
          className={cn(
            "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
            rightPanelType ? "w-[320px] opacity-100" : "w-0 opacity-0",
          )}
        >
          <div className="w-[320px] h-full p-1">
            <div className="w-full h-full rounded-md border border-border/40 bg-muted/30 backdrop-blur-xl shadow-2xl overflow-hidden animate-in slide-in-from-right-4 duration-300">
              <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                {rightPanelType === "properties" && (
                  <PropertySidebar layerType={layerType} viewData={viewData} />
                )}
                {rightPanelType === "attachments" && <AttachmentSection />}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

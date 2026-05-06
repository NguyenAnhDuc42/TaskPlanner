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
  workspaceId: string;
  entityId: string;
  layerType: EntityLayerType;
  entityInfo: any;
  views: any[];
  viewData: any;
  isLoading: boolean;
}

export type RightPanelType = "properties" | "attachments" | null;

export function LayerView({
  entityInfo,
  viewData,
  isLoading,
  layerType,
}: LayerViewProps) {
  const [activeTab, setActiveTab] = useState<MainViewTab>("overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("board");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>(null);

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType(prev => prev === type ? null : type);
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      {/* Row 1: Header (Breadcrumbs + Actions) */}
      <LayerHeader 
        entityInfo={entityInfo}
        activeTab={activeTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        rightPanelType={rightPanelType}
        onToggleRightPanel={toggleRightPanel}
      />

      {/* Row 2: Tabs (Dedicated Switcher) */}
      <LayerTabs 
        activeTab={activeTab} 
        onTabChange={setActiveTab} 
      />

      {/* Row 3: Content Area + Right Panel */}
      <div className="flex-1 flex overflow-hidden relative">
        <div className="flex-1 overflow-hidden relative">
          {activeTab === "overview" && (
            <OverviewView 
              entityInfo={entityInfo} 
              viewData={viewData} 
              layerType={layerType}
            />
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

        {/* Dynamic Right Window (Floating Panel) */}
        <div className={cn(
          "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
          rightPanelType ? "w-[300px] opacity-100" : "w-0 opacity-0"
        )}>
          <div className="w-[300px] h-full p-1">
             <div className="w-full h-full rounded-md border border-border/40 bg-muted/30 backdrop-blur-xl shadow-2xl overflow-hidden animate-in slide-in-from-right-4 duration-300">
                <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                  {rightPanelType === "properties" && (
                    <PropertySidebar layerType={layerType} viewData={viewData} />
                  )}
                  {rightPanelType === "attachments" && (
                    <AttachmentSection />
                  )}
                </div>
             </div>
          </div>
        </div>
      </div>
    </div>
  );
}

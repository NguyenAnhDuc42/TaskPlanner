import { useState } from "react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { LayerHeader } from "./components/layer-header";
import { LayerTabs } from "./components/layer-tabs";
import { OverviewView } from "./views/overview-view";
import { ItemsView } from "./views/items-view";
import type { MainViewTab, ItemsViewMode } from "./layer-detail-types";

interface LayerViewProps {
  workspaceId: string;
  entityId: string;
  layerType: EntityLayerType;
  entityInfo: any;
  views: any[];
  viewData: any;
  isLoading: boolean;
}

export function LayerView({
  entityInfo,
  viewData,
  isLoading,
  layerType,
}: LayerViewProps) {
  const [activeTab, setActiveTab] = useState<MainViewTab>("items");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("board");

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      {/* Row 1: Header (Breadcrumbs + Actions) */}
      <LayerHeader 
        entityInfo={entityInfo}
        activeTab={activeTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />

      {/* Row 2: Tabs (Dedicated Switcher) */}
      <LayerTabs 
        activeTab={activeTab} 
        onTabChange={setActiveTab} 
      />

      {/* Row 3: Content Area */}
      <div className="flex-1 overflow-hidden relative">
        {activeTab === "overview" && (
          <OverviewView 
            entityInfo={entityInfo} 
            viewData={viewData} 
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
    </div>
  );
}

import { EntityLayerType } from "@/types/entity-layer-type";
import { useParams } from "@tanstack/react-router";
import { LayerView } from "./layer-view";
import CommandCenterIndex from "../command-center/command-center-index";
import { useEntityInfo } from "../hierarchy/hierarchy-api";
import { useViews, useViewData } from "../hierarchy/views/views-api";
import { useMemo, useState } from "react";
import { ViewType } from "@/types/view-type";

interface LayerDetailIndexProps {
  forcedLayerType?: EntityLayerType;
}

export function LayerDetailIndex({ forcedLayerType }: LayerDetailIndexProps) {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, spaceId, folderId, taskId } = params;

  // Determine active entity and layer type from route
  const activeEntityId = (taskId || folderId || spaceId || "") as string;
  const activeLayerType = forcedLayerType || (taskId
    ? EntityLayerType.ProjectTask
    : folderId
      ? EntityLayerType.ProjectFolder
      : EntityLayerType.ProjectSpace);

  const entityInfo = useEntityInfo(workspaceId || "", activeEntityId);
  const { data: views } = useViews(activeEntityId, activeLayerType);
  
  // We need to decide which view's data to fetch. 
  // For the standard 'Items' tab, we want the first non-overview view.
  const itemsView = useMemo(() => 
    views?.find(v => v.viewType !== ViewType.Overview) || views?.[0], 
  [views]);

  const { data: viewResponse, isLoading: isLoadingData } = useViewData(itemsView?.id || "");

  if (!activeEntityId) {
    return <CommandCenterIndex />;
  }

  return (
    <div className="flex-1 flex overflow-hidden bg-background h-full">
      <LayerView
        workspaceId={workspaceId || ""}
        entityId={activeEntityId}
        layerType={activeLayerType}
        entityInfo={entityInfo}
        views={views || []}
        viewData={viewResponse?.data}
        isLoading={isLoadingData}
      />
    </div>
  );
}

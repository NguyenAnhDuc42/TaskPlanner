import { EntityLayerType } from "@/types/entity-layer-type";
import { useParams } from "@tanstack/react-router";
import { LayerView } from "./layer-view";
import CommandCenterIndex from "../command-center/command-center-index";
import { useEntityDetail } from "./layer-api";

import { Loader2 } from "lucide-react";
import { useLayerRealtime } from "./hooks/use-layer-realtime";

interface LayerDetailIndexProps {
  forcedLayerType?: EntityLayerType;
}

export function LayerDetailIndex({ forcedLayerType }: LayerDetailIndexProps) {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, spaceId, folderId, taskId } = params;

  // 1. Resolve Active Entity State
  const activeEntityId = (taskId || folderId || spaceId || "") as string;
  const activeLayerType = forcedLayerType || (taskId
    ? EntityLayerType.ProjectTask
    : folderId
      ? EntityLayerType.ProjectFolder
      : EntityLayerType.ProjectSpace);

  // 2. Fetch Core Data (Single API Fetch)
  const { data: viewData, isLoading, isError } = useEntityDetail(workspaceId || "", activeEntityId, activeLayerType);
  
  // 3. Register Local SignalR Listeners
  useLayerRealtime(workspaceId || "");
  
  if (!activeEntityId) {
    return <CommandCenterIndex />;
  }

  if (isLoading && !viewData) {
    return (
      <div className="flex-1 flex items-center justify-center bg-background text-primary/40">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex-1 flex items-center justify-center bg-background text-destructive/50 font-bold uppercase tracking-widest text-[10px]">
        Failed to intercept layer data
      </div>
    );
  }

  return (
    <div className="flex-1 flex overflow-hidden bg-background h-full">
      <LayerView
        key={activeEntityId} 
        workspaceId={workspaceId || ""}
        entityId={activeEntityId}
        layerType={activeLayerType}
        views={[]} 
        viewData={viewData}
        isLoading={isLoading}
      />
    </div>
  );
}

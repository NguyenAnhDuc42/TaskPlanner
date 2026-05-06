import { EntityLayerType } from "@/types/entity-layer-type";
import { useParams } from "@tanstack/react-router";
import { LayerView } from "./layer-view";
import CommandCenterIndex from "../command-center/command-center-index";
import { useEntityInfo } from "../hierarchy/hierarchy-api";
import { useEntityDetail } from "../../hooks/use-entity-detail";
import { Loader2 } from "lucide-react";

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

  const { data: viewData, isLoading, isError } = useEntityDetail(workspaceId || "", activeEntityId, activeLayerType);
  const entityInfo = useEntityInfo(workspaceId || "", activeEntityId);
  
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
        workspaceId={workspaceId || ""}
        entityId={activeEntityId}
        layerType={activeLayerType}
        entityInfo={entityInfo}
        views={[]} // Views are now decoupled or legacy
        viewData={viewData}
        isLoading={isLoading}
      />
    </div>
  );
}

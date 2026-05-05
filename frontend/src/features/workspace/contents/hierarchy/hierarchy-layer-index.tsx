import { EntityLayerType } from "@/types/entity-layer-type";
import { ViewsDisplayer } from "./views/views-displayer";
import CommandCenterIndex from "../command-center/command-center-index";
import { useParams } from "@tanstack/react-router";

export function HierarchyLayerIndex() {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, spaceId, folderId, taskId } = params;

  // Determine active entity and layer type from route
  const activeEntityId = (taskId || folderId || spaceId || "") as string;
  const activeLayerType = taskId
    ? EntityLayerType.ProjectTask
    : folderId
      ? EntityLayerType.ProjectFolder
      : EntityLayerType.ProjectSpace;

  return (
    <div className="flex-1 flex overflow-hidden bg-background h-full">
      {activeEntityId ? (
        <ViewsDisplayer
          workspaceId={workspaceId || ""}
          entityId={activeEntityId}
          layerType={activeLayerType}
        />
      ) : (
        <CommandCenterIndex />
      )}
    </div>
  );
}

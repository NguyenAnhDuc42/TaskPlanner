import { useParams } from "@tanstack/react-router";
import { EntityLayerType } from "@/types/relationship-type";
import { ViewsDisplayer } from "./views/views-displayer";

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
    <div className="flex-1 flex overflow-hidden bg-background/50 h-full">
      {activeEntityId ? (
        <ViewsDisplayer 
          workspaceId={workspaceId || ""}
          entityId={activeEntityId}
          layerType={activeLayerType}
        />
      ) : (
        <div className="flex-1 flex items-center justify-center opacity-10 select-none">
          <div className="flex flex-col items-center gap-4">
            <div className="h-24 w-24 rounded-full border-4 border-dashed border-foreground/50" />
            <span className="text-xl font-black uppercase tracking-widest">Select Layer</span>
          </div>
        </div>
      )}
    </div>
  );
}

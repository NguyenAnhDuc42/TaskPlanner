import { EntityLayerType } from "@/types/relationship-type";
import { TaskListView } from "./view-components/list-view/list-view";
import { TaskBoardView } from "./view-components/board-view/board-view";
import { TaskInfoView } from "./view-components/task/task-info-view";
import { ViewType } from "@/types/view-type";
import type { TaskListViewResult, TasksBoardViewResult, ViewDto } from "./views-type";

interface ViewContainerProps {
  workspaceId: string;
  layerId: string;
  layerType: EntityLayerType;
  data: any;
  view: ViewDto;
}

export function ViewContainer({ 
  workspaceId, 
  layerId, 
  layerType, 
  data, 
  view 
}: ViewContainerProps) {
  
  // Dispatch based on Layer Type and View Type
  
  // Exception: Task Layer has a specialized Info View
  if (layerType === EntityLayerType.ProjectTask) {
    return (
      <div className="h-full overflow-hidden">
        <TaskInfoView data={data} taskId={layerId} />
      </div>
    );
  }

  // Collections (Space / Folder) dispatch to specific view implementations
  return (
    <div className="h-full overflow-hidden">
      {view.viewType === ViewType.List && (
        <TaskListView
          data={data as TaskListViewResult}
          view={view}
          workspaceId={workspaceId}
          layerId={layerId}
          layerType={layerType}
        />
      )}
      {view.viewType === ViewType.Board && (
        <TaskBoardView
          data={data as TasksBoardViewResult}
          view={view}
          workspaceId={workspaceId}
          layerId={layerId}
          layerType={layerType}
        />
      )}
      {/* Fallback for unhandled view types */}
      {view.viewType !== ViewType.List && view.viewType !== ViewType.Board && (
        <div className="flex flex-col items-center justify-center h-full text-muted-foreground/20">
           <div className="h-20 w-20 border-2 border-dashed border-white/5 rounded-3xl mb-4 animate-pulse" />
           <span className="text-[10px] font-black uppercase tracking-widest">{view.name} Layout Orchestration...</span>
        </div>
      )}
    </div>
  );
}

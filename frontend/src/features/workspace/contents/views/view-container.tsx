import { useParams } from "@tanstack/react-router";
import { useViews, useViewData } from "./views-api";
import { useState, useEffect } from "react";
import type { ViewDto } from "./views-type";
import { TaskListView } from "./view-components/list-view";
import { TaskBoardView } from "./view-components/board-view";
import { ViewType } from "@/types/view-type";
import type { TaskListViewResult, TasksBoardViewResult } from "./views-type";
import { Loader2 } from "lucide-react";
import { ViewTabBar } from "./view-tab-bar";
import { ViewOptionsBar } from "./view-options-bar";

interface ViewContainerProps {
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
}

export function ViewContainer({ layerType }: ViewContainerProps) {
  const params = useParams({ strict: false });
  const layerId = (
    layerType === "ProjectSpace"
      ? params.spaceId
      : layerType === "ProjectFolder"
        ? params.folderId
        : params.listId
  ) as string;

  const { data: views, isLoading: isViewsLoading } = useViews(
    layerId,
    layerType,
  );
  const [activeView, setActiveView] = useState<ViewDto | null>(null);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    if (views && views.length > 0 && !activeView) {
      const defaultView = views.find((v) => v.isDefault) || views[0];
      setActiveView(defaultView);
    }
  }, [views]);

  const { data: viewData, isLoading: isDataLoading } = useViewData(
    activeView?.id || "",
  );

  if (isViewsLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <ViewTabBar
        views={views || []}
        activeViewId={activeView?.id || null}
        onViewChange={(v) => setActiveView(v)}
        layerId={layerId}
        layerType={layerType}
      />

      {activeView && (
        <ViewOptionsBar
          view={activeView}
          layerId={layerId}
          layerType={layerType}
        />
      )}

      <div className="flex-1 overflow-auto p-4">
        {isDataLoading ? (
          <div className="flex items-center justify-center h-full">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : activeView && viewData ? (
          <>
            {activeView.viewType === ViewType.List && (
              <TaskListView
                data={viewData as TaskListViewResult}
                view={activeView}
              />
            )}
            {activeView.viewType === ViewType.Board && (
              <TaskBoardView
                data={viewData as TasksBoardViewResult}
                view={activeView}
              />
            )}
            {/* Add more types as implemented */}
          </>
        ) : (
          <div className="text-center text-muted-foreground mt-20">
            No active view selected.
          </div>
        )}
      </div>
    </div>
  );
}

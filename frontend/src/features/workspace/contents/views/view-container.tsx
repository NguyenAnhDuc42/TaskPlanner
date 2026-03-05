import { useParams } from "@tanstack/react-router";
import { useViews, useViewData } from "./views-api";
import { useMemo, useState } from "react";
import { TaskListView } from "./view-components/list-view/list-view";
import { TaskBoardView } from "./view-components/board-view/board-view";
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

  const { data: views, isLoading: isViewsLoading } = useViews(layerId, layerType);
  const [preferredViewIdByLayer, setPreferredViewIdByLayer] = useState<
    Record<string, string>
  >({});
  const layerKey = `${layerType}:${layerId}`;

  const activeViewId = useMemo(() => {
    if (!views || views.length === 0) return null;
    const preferred = preferredViewIdByLayer[layerKey];
    if (preferred && views.some((view) => view.id === preferred)) {
      return preferred;
    }
    return (views.find((view) => view.isDefault) ?? views[0]).id;
  }, [views, preferredViewIdByLayer, layerKey]);

  const activeView = views?.find((v) => v.id === activeViewId) ?? null;

  const { data: viewData, isLoading: isDataLoading } = useViewData(
    activeViewId || "",
  );

  if (isViewsLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const workspaceId = params.workspaceId as string;
  const listId = layerType === "ProjectList" ? layerId : undefined;

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <ViewTabBar
        views={views || []}
        activeViewId={activeViewId}
        onViewChange={(v) =>
          setPreferredViewIdByLayer((prev) => ({ ...prev, [layerKey]: v.id }))
        }
        layerId={layerId}
        layerType={layerType}
      />

      {activeView && (
        <ViewOptionsBar
          view={activeView}
          layerId={layerId}
          layerType={layerType}
          workspaceId={workspaceId}
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
                workspaceId={workspaceId}
                layerId={layerId}
                layerType={layerType}
                listId={listId}
              />
            )}
            {activeView.viewType === ViewType.Board && (
              <TaskBoardView
                data={viewData as TasksBoardViewResult}
                view={activeView}
                workspaceId={workspaceId}
                layerId={layerId}
                layerType={layerType}
                listId={listId}
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

import { EntityLayerType } from "@/types/entity-layer-type";
import { useEntityInfo, useHierarchy } from "../hierarchy-api";
import { useViews, useViewData } from "./views-api";
import { useState, useMemo, useEffect } from "react";
import { ViewHeader } from "./view-components/layout/view-header";
import { SpaceViewSwitcher } from "./view-components/layers/space/space-view-switcher";
import { FolderViewSwitcher } from "./view-components/layers/folder/folder-view-switcher";
import { useQueryClient } from "@tanstack/react-query";
import { viewsKeys } from "./views-keys";
import { hierarchyKeys } from "../hierarchy-keys";
import CommandCenterIndex from "../../command-center/command-center-index";


interface ViewsDisplayerProps {
  workspaceId: string;
  entityId: string;
  layerType: EntityLayerType;
}

export function ViewsDisplayer({
  workspaceId,
  entityId,
  layerType,
}: ViewsDisplayerProps) {
  const queryClient = useQueryClient();
  const { isLoading: isLoadingHierarchy, isFetched: isHierarchyFetched } = useHierarchy(workspaceId);
  const entityInfo = useEntityInfo(workspaceId, entityId);
  const [isContextOpen, setIsContextOpen] = useState(true);

  const { data: views, isLoading: isLoadingViews, refetch: refetchViews } = useViews(entityId, layerType);
  const [activeViewId, setActiveViewId] = useState<string | null>(null);

  useEffect(() => {
    const handleCreated = (data: any) => {
      if (data.SpaceId === entityId || data.FolderId === entityId) {
        refetchViews();
      }
    };

    const handleUpdated = (data: any) => {
      queryClient.invalidateQueries({ queryKey: viewsKeys.all });
      if (data.SpaceId === entityId || data.FolderId === entityId || data.TaskId === entityId) {
         queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
      }
    };

    const signalR = import("@/lib/signalr-service").then(m => {
      m.signalRService.on("SpaceCreated", handleCreated);
      m.signalRService.on("FolderCreated", handleCreated);
      m.signalRService.on("SpaceUpdated", handleUpdated);
      m.signalRService.on("FolderUpdated", handleUpdated);
      m.signalRService.on("TaskUpdated", handleUpdated);
      return m.signalRService;
    });

    return () => {
      signalR.then(s => {
        s.off("SpaceCreated", handleCreated);
        s.off("FolderCreated", handleCreated);
        s.off("SpaceUpdated", handleUpdated);
        s.off("FolderUpdated", handleUpdated);
        s.off("TaskUpdated", handleUpdated);
      });
    };
  }, [entityId, refetchViews, queryClient, workspaceId]);

  useEffect(() => {
    if (views && views.length > 0) {
      const isCurrentViewValid = activeViewId && views.some(v => v.id === activeViewId);
      if (!isCurrentViewValid) {
        const defaultView = views.find(v => v.isDefault) || views[0];
        setActiveViewId(defaultView.id);
      }
    }
  }, [views, activeViewId]);

  const { data: viewResponse, isLoading: isLoadingData } = useViewData(activeViewId || "");

  const activeView = useMemo(
    () => views?.find((v) => v.id === activeViewId),
    [views, activeViewId],
  );

  const isLoading = isLoadingHierarchy || isLoadingViews || (isLoadingData && !viewResponse);

  // --- Fallback Check ---
  // If hierarchy is loaded but entityInfo is null, it's a 404
  if (isHierarchyFetched && !entityInfo && !isLoadingHierarchy) {
    return <CommandCenterIndex isFallback={true} />;
  }

  const viewHeader = (
    <ViewHeader
      entityName={entityInfo?.name || ""}
      entityType={entityInfo?.type || ""}
      parentName={(entityInfo as any)?.parentName}
      views={views}
      activeViewId={activeViewId}
      onViewChange={setActiveViewId}
      isContextOpen={isContextOpen}
      onContextToggle={() => setIsContextOpen(!isContextOpen)}
    />
  );

  if (layerType === EntityLayerType.ProjectSpace) {
    return (
      <SpaceViewSwitcher 
        workspaceId={workspaceId} 
        entityId={entityId} 
        view={activeView || null} 
        data={viewResponse?.data as any} 
        viewHeader={viewHeader}
        isLoading={isLoading}
        isContextOpen={isContextOpen}
        setIsContextOpen={setIsContextOpen}
        entityInfo={entityInfo}
      />
    );
  }

  if (layerType === EntityLayerType.ProjectFolder) {
    return (
      <FolderViewSwitcher 
        workspaceId={workspaceId} 
        entityId={entityId} 
        view={activeView || null} 
        data={viewResponse?.data as any} 
        viewHeader={viewHeader}
        isLoading={isLoading}
        isContextOpen={isContextOpen}
        setIsContextOpen={setIsContextOpen}
        entityInfo={entityInfo}
      />
    );
  }

  return (
    <div className="flex-1 flex items-center justify-center text-muted-foreground/10 text-[10px] font-black uppercase tracking-[1em]">
      {layerType} Implementation Pending...
    </div>
  );
}

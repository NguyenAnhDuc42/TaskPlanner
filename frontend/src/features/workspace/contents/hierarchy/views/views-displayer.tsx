import { EntityLayerType } from "@/types/entity-layer-type";
import { useEntityInfo } from "../hierarchy-api";
import { useViews, useViewData } from "./views-api";
import { useState, useMemo, useEffect } from "react";
import { Loader2 } from "lucide-react";

import { ViewHeader } from "./view-components/layout/view-header";
import { SplitView } from "./view-components/layout/split-view";
import { SpaceViewSwitcher } from "./view-components/layers/space/space-view-switcher";
import { FolderViewSwitcher } from "./view-components/layers/folder/folder-view-switcher";

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
  const entityInfo = useEntityInfo(workspaceId, entityId);
  const [isContextOpen, setIsContextOpen] = useState(true);

  const { data: views, isLoading: isLoadingViews, refetch: refetchViews } = useViews(entityId, layerType);
  const [activeViewId, setActiveViewId] = useState<string | null>(null);

  useEffect(() => {
    const handleReady = (data: any) => {
      if (data.SpaceId === entityId || data.FolderId === entityId) {
        refetchViews();
      }
    };

    const signalR = import("@/lib/signalr-service").then(m => {
      m.signalRService.on("SpaceReady", handleReady);
      m.signalRService.on("FolderReady", handleReady);
      return m.signalRService;
    });

    return () => {
      signalR.then(s => {
        s.off("SpaceReady", handleReady);
        s.off("FolderReady", handleReady);
      });
    };
  }, [entityId, refetchViews]);

  useEffect(() => {
    if (views && views.length > 0) {
      const isCurrentViewValid = activeViewId && views.some(v => v.id === activeViewId);
      if (!isCurrentViewValid) {
        const defaultView = views.find(v => v.isDefault) || views[0];
        setActiveViewId(defaultView.id);
      }
    }
  }, [views, activeViewId]);

  // Real View Data
  const { data: viewResponse, isLoading: isLoadingData } = useViewData(activeViewId || "");

  const activeView = useMemo(
    () => views?.find((v) => v.id === activeViewId),
    [views, activeViewId],
  );

  if (!entityInfo) return null;

  const viewHeader = (
    <ViewHeader
      entityName={entityInfo.name}
      entityType={entityInfo.type}
      parentName={(entityInfo as any).parentName}
      views={views}
      activeViewId={activeViewId}
      onViewChange={setActiveViewId}
      isContextOpen={isContextOpen}
      onContextToggle={() => setIsContextOpen(!isContextOpen)}
    />
  );

  if (isLoadingViews || (isLoadingData && !viewResponse)) {
    return (
      <SplitView
        left={
          <div className="flex flex-col h-full w-full">
            {viewHeader}
            <div className="flex-1 flex items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground/30" />
            </div>
          </div>
        }
        right={null}
        isRightOpen={false}
      />
    );
  }

  if (!activeView || !viewResponse) {
    return (
      <SplitView
        left={
          <div className="flex flex-col h-full w-full">
            {viewHeader}
            <div className="flex-1 flex items-center justify-center text-muted-foreground/40 text-[12px] uppercase font-semibold tracking-wider">
              No Active View Content
            </div>
          </div>
        }
        right={null}
        isRightOpen={false}
      />
    );
  }

  if (layerType === EntityLayerType.ProjectSpace) {
    return (
      <SpaceViewSwitcher 
        workspaceId={workspaceId} 
        entityId={entityId} 
        view={activeView} 
        data={viewResponse.data as any} 
        viewHeader={viewHeader}
        isContextOpen={isContextOpen}
        setIsContextOpen={setIsContextOpen}
      />
    );
  }

  if (layerType === EntityLayerType.ProjectFolder) {
    return (
      <FolderViewSwitcher 
        workspaceId={workspaceId} 
        entityId={entityId} 
        view={activeView} 
        data={viewResponse.data as any} 
        viewHeader={viewHeader}
        isContextOpen={isContextOpen}
        setIsContextOpen={setIsContextOpen}
      />
    );
  }

  return (
    <div className="flex-1 flex items-center justify-center text-muted-foreground/10 text-[10px] font-black uppercase tracking-[1em]">
      {layerType} Implementation Pending...
    </div>
  );
}

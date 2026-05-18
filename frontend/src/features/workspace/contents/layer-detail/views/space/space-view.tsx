import { useState, useEffect, useCallback, useRef } from "react";
import { SpaceHeader } from "./space-header";
import { LayerTabs } from "../../components/layer-tabs";
import { SpaceOverview } from "./space-overview";
import { SpaceItemsView } from "./space-items-view";
import { SpaceSidebar } from "./space-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import {useSpaceItems } from "./space-api";
import { useWorkspaceWorkflows } from "@/features/workspace/api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useSpaceRealtime } from "./space-realtime";
import { useTaskRealtime } from "../task/task-realtime";
import { useFolderRealtime } from "../folder/folder-realtime";
import { LoadingComponent } from "@/components/loading-component";
import { useNavigate } from "@tanstack/react-router";
import { useDeleteSpace } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import { SpaceEditorProvider, } from "./space-editor-context";

interface SpaceViewProps {
  workspaceId: string;
  spaceId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function SpaceView({ workspaceId, spaceId }: SpaceViewProps) {
  return (
    <SpaceEditorProvider workspaceId={workspaceId} spaceId={spaceId}>
      <SpaceViewContent workspaceId={workspaceId} spaceId={spaceId} />
    </SpaceEditorProvider>
  );
}

function SpaceViewContent({ workspaceId, spaceId }: SpaceViewProps) {
  const { data: itemsData, isLoading: itemsLoading } = useSpaceItems(spaceId);
  useSpaceRealtime(workspaceId);
  useFolderRealtime(workspaceId);
  useTaskRealtime(workspaceId);
  useWorkspaceWorkflows(workspaceId);
  
  const [activeTab, setActiveTab] = useState<MainViewTab>(() => (localStorage.getItem("spaceTab") as MainViewTab) || "overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>(() => (localStorage.getItem("spaceViewMode") as ItemsViewMode) || "list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  useEffect(() => { localStorage.setItem("spaceTab", activeTab); }, [activeTab]);
  useEffect(() => { localStorage.setItem("spaceViewMode", viewMode); }, [viewMode]);

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  const { mutate: deleteSpace } = useDeleteSpace(workspaceId);
  const navigate = useNavigate();

  const handleDelete = () => {
    if (window.confirm("Are you sure you want to delete this space?")) {
      deleteSpace(spaceId, {
        onSuccess: () => {
          navigate({ to: `/workspaces/${workspaceId}` });
        },
      });
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <SpaceHeader
        onDelete={handleDelete}
        activeTab={activeTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        rightPanelType={rightPanelType}
        onToggleRightPanel={toggleRightPanel}
      />

      <LayerTabs
        activeTab={activeTab}
        onTabChange={setActiveTab}
        layerType={EntityLayerType.ProjectSpace}
      />

      <div className="flex-1 flex relative">
        <div className="flex-1 relative min-w-0">
          {activeTab === "overview" && (
            <SpaceOverview />
          )}
          {activeTab === "items" && (
            itemsLoading ? (
              <LoadingComponent />
            ) : !itemsData ? (
              <div>No items found</div>
            ) : (
              <SpaceItemsView
                viewData={itemsData}
                spaceId={spaceId}
                viewMode={viewMode}
              />
            )
          )}
        </div>

        <div
          className={cn(
            "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
            (rightPanelType && activeTab === "overview") ? "w-[320px] opacity-100" : "w-0 opacity-0",
          )}
        >
          <div className="w-[320px] h-full p-1">
            <div className="w-full h-full rounded-md border border-border/40 bg-background/95 backdrop-blur-md shadow-2xl overflow-hidden duration-300">
              <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                {rightPanelType === "properties" && (
                  <SpaceSidebar />
                )}
                {rightPanelType === "attachments" && <AttachmentSection />}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

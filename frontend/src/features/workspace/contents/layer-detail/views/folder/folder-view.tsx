import { useState, useEffect, useCallback, useRef } from "react";
import { FolderHeader } from "./folder-header";
import { LayerTabs } from "../../components/layer-tabs";
import { FolderOverview } from "./folder-overview";

import { FolderItemsView } from "./folder-items-view";
import { FolderSidebar } from "./folder-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { LoadingComponent } from "@/components/loading-component";
import { useNavigate } from "@tanstack/react-router";
import { useDeleteFolder } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import { useFolderDetail, useUpdateFolder, useFolderItems } from "./folder-api";
import { useWorkspaceWorkflows } from "@/features/workspace/api";
import { useDebounce } from "@/hooks/use-debounce";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useFolderRealtime } from "./folder-realtime";
import { useTaskRealtime } from "../task/task-realtime";
import { FolderEditorProvider, useFolderEditor } from "./folder-editor-context";

interface FolderViewProps {
  workspaceId: string;
  folderId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function FolderView({ workspaceId, folderId }: FolderViewProps) {
  return (
    <FolderEditorProvider workspaceId={workspaceId} folderId={folderId}>
      <FolderViewContent workspaceId={workspaceId} folderId={folderId} />
    </FolderEditorProvider>
  );
}

function FolderViewContent({ workspaceId, folderId }: FolderViewProps) {
  const { data: itemsData, isLoading: itemsLoading } = useFolderItems(folderId);
  useFolderRealtime(workspaceId);
  useTaskRealtime(workspaceId);
  useWorkspaceWorkflows(workspaceId);
  
  const [activeTab, setActiveTab] = useState<MainViewTab>(() => (localStorage.getItem("folderTab") as MainViewTab) || "overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>(() => (localStorage.getItem("folderViewMode") as ItemsViewMode) || "list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  useEffect(() => { localStorage.setItem("folderTab", activeTab); }, [activeTab]);
  useEffect(() => { localStorage.setItem("folderViewMode", viewMode); }, [viewMode]);

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  const { mutate: deleteFolder } = useDeleteFolder(workspaceId);
  const navigate = useNavigate();

  const handleDelete = () => {
    if (window.confirm("Are you sure you want to delete this folder?")) {
      deleteFolder(folderId, {
        onSuccess: () => {
          navigate({ to: `/workspaces/${workspaceId}` });
        },
      });
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <FolderHeader
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
        layerType={EntityLayerType.ProjectFolder}
      />

      <div className="flex-1 flex relative">
        <div className="flex-1 relative min-w-0">
          {activeTab === "overview" && (
            <FolderOverview />
          )}
          {activeTab === "items" && (
            itemsLoading ? (
              <LoadingComponent />
            ) : !itemsData ? (
              <div>No items found</div>
            ) : (
              <FolderItemsView
                viewData={itemsData}
                folderId={folderId}
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
            <div className="w-full h-full rounded-md border border-border/40 bg-background/95 backdrop-blur-xl shadow-2xl overflow-hidden duration-300">
              <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                {rightPanelType === "properties" && (
                  <FolderSidebar />
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

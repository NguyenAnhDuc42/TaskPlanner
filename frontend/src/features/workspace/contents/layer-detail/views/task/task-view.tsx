import { useState } from "react";
import { TaskHeader } from "./task-header";
import { LayerTabs } from "../../components/layer-tabs";
import { TaskDetailView } from "./task-detail-view";
import { TaskSidebar } from "./task-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { useWorkspaceWorkflows } from "@/features/workspace/api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useTaskRealtime } from "./task-realtime";
import { TaskEditorProvider } from "./task-editor-context";

interface TaskViewProps {
  workspaceId: string;
  taskId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function TaskView({ workspaceId, taskId }: TaskViewProps) {
  useTaskRealtime(workspaceId);
  useWorkspaceWorkflows(workspaceId);
  const [activeTab, setActiveTab] = useState<MainViewTab>("overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  return (
    <TaskEditorProvider workspaceId={workspaceId} taskId={taskId}>
      <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
        <TaskHeader
          activeTab={activeTab}
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          rightPanelType={rightPanelType}
          onToggleRightPanel={toggleRightPanel}
        />

        <LayerTabs
          activeTab={activeTab}
          onTabChange={setActiveTab}
          layerType={EntityLayerType.ProjectTask}
        />

        <div className="flex-1 flex overflow-hidden relative">
          <div className="flex-1 overflow-hidden relative">
            {activeTab === "overview" && (
              <TaskDetailView />
            )}
          </div>

          <div
            className={cn(
              "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
              rightPanelType ? "w-[320px] opacity-100" : "w-0 opacity-0",
            )}
          >
            <div className="w-[320px] h-full p-1">
              <div className="w-full h-full rounded-md border border-border/40 bg-muted/30 backdrop-blur-xl shadow-2xl overflow-hidden animate-in slide-in-from-right-4 duration-300">
                <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                  {rightPanelType === "properties" && (
                    <TaskSidebar />
                  )}
                  {rightPanelType === "attachments" && <AttachmentSection />}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </TaskEditorProvider>
  );
}

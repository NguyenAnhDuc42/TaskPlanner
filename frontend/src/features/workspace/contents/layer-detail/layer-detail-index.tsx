import { EntityLayerType } from "@/types/entity-layer-type";
import { useParams } from "@tanstack/react-router";
import { LayerView } from "./layer-view";
import CommandCenterIndex from "../command-center/command-center-index";
import { useEntityInfo } from "../hierarchy/hierarchy-api";
import { useMemo } from "react";

interface LayerDetailIndexProps {
  forcedLayerType?: EntityLayerType;
}

export function LayerDetailIndex({ forcedLayerType }: LayerDetailIndexProps) {
  const params = useParams({ strict: false }) as any;
  const { workspaceId, spaceId, folderId, taskId } = params;

  // Determine active entity and layer type from route
  const activeEntityId = (taskId || folderId || spaceId || "") as string;
  const activeLayerType = forcedLayerType || (taskId
    ? EntityLayerType.ProjectTask
    : folderId
      ? EntityLayerType.ProjectFolder
      : EntityLayerType.ProjectSpace);

  const entityInfo = useEntityInfo(workspaceId || "", activeEntityId);
  
  // Mock ViewData for the new high-density Overview
  const mockViewData = useMemo(() => ({
    status: { name: "In Progress", color: "#3b82f6" },
    priority: "High",
    startDate: new Date().toISOString(),
    dueDate: new Date(Date.now() + 86400000 * 7).toISOString(), // 7 days from now
    workflowName: "Standard Development",
    storyPoints: 8,
    timeEstimate: 480, // 8 hours
    progress: { completedTasks: 12, totalTasks: 20 },
    assignees: [
      { id: "1", name: "Duc" },
      { id: "2", name: "Gemini" }
    ],
    recentActivity: [
      { id: "1", content: "Updated operational scope", timestamp: new Date().toISOString() },
      { id: "2", content: "Attached System_Architecture.png", timestamp: new Date(Date.now() - 3600000).toISOString() },
      { id: "3", content: "Changed status to In Progress", timestamp: new Date(Date.now() - 7200000).toISOString() },
    ]
  }), []);

  if (!activeEntityId) {
    return <CommandCenterIndex />;
  }

  return (
    <div className="flex-1 flex overflow-hidden bg-background h-full">
      <LayerView
        workspaceId={workspaceId || ""}
        entityId={activeEntityId}
        layerType={activeLayerType}
        entityInfo={entityInfo}
        views={[]} // Views are now decoupled or legacy
        viewData={mockViewData}
        isLoading={false}
      />
    </div>
  );
}

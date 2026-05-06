import { useMemo } from "react";
import { 
  useSpaceDetail, 
  useFolderDetail, 
  useTaskDetail 
} from "../api";
import { useWorkspace } from "../context/workspace-provider";
import { EntityLayerType } from "@/types/entity-layer-type";


export function useEntityDetail(workspaceId: string, entityId: string, type: EntityLayerType) {
  const { registry } = useWorkspace();
  
  const spaceQuery = useSpaceDetail(workspaceId, entityId, type === EntityLayerType.ProjectSpace);
  const folderQuery = useFolderDetail(workspaceId, entityId, type === EntityLayerType.ProjectFolder);
  const taskQuery = useTaskDetail(workspaceId, entityId, type === EntityLayerType.ProjectTask);

  // Determine which query to use based on type
  const activeQuery = useMemo(() => {
    switch (type) {
      case EntityLayerType.ProjectSpace: return spaceQuery;
      case EntityLayerType.ProjectFolder: return folderQuery;
      case EntityLayerType.ProjectTask: return taskQuery;
      default: return null;
    }
  }, [type, spaceQuery, folderQuery, taskQuery]);

  // Enrich the data with registry metadata
  const enrichedData = useMemo(() => {
    if (!activeQuery?.data) return null;
    
    const data = activeQuery.data as any;
    
    // Map status
    const status = data.statusId ? registry.statusMap[data.statusId] : null;
    
    // Map members/assignees
    const memberIds = data.memberIds || data.assigneeIds || [];
    const members = memberIds.map((id: string) => registry.memberMap[id]).filter(Boolean);

    return {
      ...data,
      status,
      members,
      // For tasks, we also want to expose assignees as members for the UI
      assignees: members
    };
  }, [activeQuery?.data, registry]);

  return {
    data: enrichedData,
    isLoading: activeQuery?.isLoading || false,
    isError: activeQuery?.isError || false,
    error: activeQuery?.error
  };
}

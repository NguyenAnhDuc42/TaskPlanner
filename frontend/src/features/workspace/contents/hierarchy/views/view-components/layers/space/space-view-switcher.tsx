import { useState, useMemo } from "react";
import { SplitView } from "../../layout/split-view";
import { SpaceOverviewMain } from "./overview-view/space-overview-main";
import { SpaceOverviewContext } from "./overview-view/space-overview-context";
import { SpaceTasksMain } from "./tasks-view/space-tasks-main";
import { FolderDrillContext } from "./tasks-view/folder-drill-context";
import { SpaceTaskFocusContext } from "./tasks-view/space-task-focus-context";
import type { TaskViewData, OverviewViewData, ViewDto, FolderItemDto, TaskItemDto } from "../../../views-type";
import { ViewType } from "@/types/view-type";

interface SpaceViewSwitcherProps {
  workspaceId: string;
  entityId: string;
  view: ViewDto;
  data: TaskViewData | OverviewViewData | any;
  viewHeader: React.ReactNode;
  isContextOpen: boolean;
  setIsContextOpen: (open: boolean) => void;
}

export function SpaceViewSwitcher({
  view,
  data,
  viewHeader,
  isContextOpen,
  setIsContextOpen,
}: SpaceViewSwitcherProps) {
  const [selection, setSelection] = useState<{
    id: string;
    type: "Folder" | "Task";
    name: string;
  } | null>(null);

  const isOverview = view.viewType === ViewType.Overview;
  
  const overviewData = isOverview ? (data as OverviewViewData) : null;
  const taskData = !isOverview ? (data as TaskViewData) : null;

  const handleFolderSelect = (folder: FolderItemDto) => {
    setSelection({ id: folder.id, type: "Folder", name: folder.name });
    setIsContextOpen(true);
  };

  const handleTaskSelect = (task: TaskItemDto) => {
    setSelection({ id: task.id, type: "Task", name: task.name });
    setIsContextOpen(true);
  };

  const mainContent = useMemo(() => {
    if (isOverview && overviewData) {
      return (
        <SpaceOverviewMain 
          name={overviewData.name} 
          description={overviewData.description} 
        />
      );
    }
    
    if (taskData) {
      return (
        <SpaceTasksMain 
          data={taskData} 
          onFolderSelect={handleFolderSelect} 
          onTaskSelect={handleTaskSelect}
          selectedId={selection?.id}
        />
      );
    }
    
    return null;
  }, [isOverview, overviewData, taskData, selection]);

  const leftSide = (
    <>
      {viewHeader}
      {mainContent}
    </>
  );

  const rightSide = useMemo(() => {
    if (isOverview && !selection) {
      return <SpaceOverviewContext />;
    }

    if (selection?.type === "Folder") {
      return (
        <FolderDrillContext 
          folderId={selection.id} 
          folderName={selection.name} 
          onTaskSelect={handleTaskSelect} 
        />
      );
    }

    if (selection?.type === "Task") {
      return (
        <SpaceTaskFocusContext 
          taskId={selection.id} 
          taskName={selection.name} 
        />
      );
    }

    return <SpaceOverviewContext />;
  }, [isOverview, selection]);

  return (
    <SplitView left={leftSide} right={rightSide} isRightOpen={isContextOpen} />
  );
}

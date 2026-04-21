import { useState, useMemo } from "react";
import { SplitView } from "../../layout/split-view";
import { FolderOverviewMain } from "./overview-view/folder-overview-main";
import { FolderOverviewContext } from "./overview-view/folder-overview-context";
import { FolderTasksMain } from "./tasks-view/folder-tasks-main";
import { TaskFocusContext } from "./tasks-view/task-focus-context";
import type { TaskViewData, OverviewViewData, ViewDto, TaskItemDto } from "../../../views-type";
import { ViewType } from "@/types/view-type";

interface FolderViewSwitcherProps {
  workspaceId: string;
  entityId: string;
  view: ViewDto;
  data: TaskViewData | OverviewViewData | any;
  viewHeader: React.ReactNode;
  isContextOpen: boolean;
  setIsContextOpen: (open: boolean) => void;
}

export function FolderViewSwitcher({
  view,
  data,
  viewHeader,
  isContextOpen,
  setIsContextOpen,
}: FolderViewSwitcherProps) {
  const [selection, setSelection] = useState<{
    id: string;
    type: "Task";
    name: string;
  } | null>(null);

  const isOverview = view.viewType === ViewType.Overview;

  const overviewData = isOverview ? (data as OverviewViewData) : null;
  const taskData = !isOverview ? (data as TaskViewData) : null;

  const handleTaskSelect = (task: TaskItemDto) => {
    setSelection({ id: task.id, type: "Task", name: task.name });
    setIsContextOpen(true);
  };

  const mainContent = useMemo(() => {
    if (isOverview && overviewData) {
      return (
        <FolderOverviewMain 
          name={overviewData.name} 
          description={overviewData.description} 
        />
      );
    }
    
    if (taskData) {
      return (
        <FolderTasksMain 
          data={taskData} 
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
      return <FolderOverviewContext />;
    }

    if (selection?.type === "Task") {
      return (
        <TaskFocusContext 
          taskId={selection.id} 
          taskName={selection.name} 
        />
      );
    }

    return <FolderOverviewContext />;
  }, [isOverview, selection]);

  return (
    <SplitView left={leftSide} right={rightSide} isRightOpen={isContextOpen} />
  );
}

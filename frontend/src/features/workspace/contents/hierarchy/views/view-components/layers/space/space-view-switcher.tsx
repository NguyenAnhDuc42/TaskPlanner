import { useState, useMemo, useEffect } from "react";
import { SplitView } from "../../layout/split-view";
import { SpaceOverviewMain } from "./overview-view/space-overview-main";
import { SpaceOverviewContext } from "./overview-view/space-overview-context";
import { SpaceTasksMain } from "./tasks-view/space-tasks-main";
import { FolderDrillContext } from "./tasks-view/folder-drill-context";
import { SpaceTaskFocusContext } from "./tasks-view/space-task-focus-context";
import type { TaskViewData, OverviewViewData, ViewDto, FolderItemDto, TaskItemDto } from "../../../views-type";
import { ViewType } from "@/types/view-type";
import { motion, AnimatePresence } from "framer-motion";
import { Loader2 } from "lucide-react";

interface SpaceViewSwitcherProps {
  workspaceId: string;
  entityId: string;
  view: ViewDto | null;
  data: TaskViewData | OverviewViewData | any;
  viewHeader: React.ReactNode;
  isLoading?: boolean;
  isContextOpen: boolean;
  setIsContextOpen: (open: boolean) => void;
}

export function SpaceViewSwitcher({
  view,
  data,
  viewHeader,
  isLoading,
  isContextOpen,
  setIsContextOpen,
}: SpaceViewSwitcherProps) {
  const [selection, setSelection] = useState<{
    id: string;
    type: "Folder" | "Task";
    name: string;
  } | null>(null);

  // Reset selection when switching views
  useEffect(() => {
    setSelection(null);
  }, [view?.id, view?.viewType]);

  const isOverview = view?.viewType === ViewType.Overview;
  
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
    if (isLoading) {
      return (
        <motion.div
          key="loading"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
          className="flex-1 flex items-center justify-center"
        >
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground/30" />
        </motion.div>
      );
    }

    if (!view || !data) {
      return (
        <motion.div
          key="empty"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="flex-1 flex items-center justify-center text-muted-foreground/40 text-[12px] uppercase font-semibold tracking-wider"
        >
          No Content Available
        </motion.div>
      );
    }

    if (isOverview && overviewData) {
      return (
        <motion.div
          key="overview"
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -4 }}
          transition={{ duration: 0.2 }}
          className="flex-1 flex flex-col min-h-0"
        >
          <SpaceOverviewMain 
            name={overviewData.name} 
            description={overviewData.description} 
          />
        </motion.div>
      );
    }
    
    if (taskData) {
      return (
        <motion.div
          key="tasks"
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -4 }}
          transition={{ duration: 0.2 }}
          className="flex-1 flex flex-col min-h-0"
        >
          <SpaceTasksMain 
            data={taskData} 
            onFolderSelect={handleFolderSelect} 
            onTaskSelect={handleTaskSelect}
            selectedId={selection?.id}
          />
        </motion.div>
      );
    }
    
    return null;
  }, [isLoading, view, data, isOverview, overviewData, taskData, selection]);

  const leftSide = (
    <div className="flex flex-col h-full w-full overflow-hidden">
      {viewHeader}
      <AnimatePresence mode="wait">
        {mainContent}
      </AnimatePresence>
    </div>
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

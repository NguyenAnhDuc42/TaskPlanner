import { useState, useMemo, useEffect } from "react";
import { SplitView } from "../../layout/split-view";
import { FolderOverviewMain } from "./overview-view/folder-overview-main";
import { FolderOverviewContext } from "./overview-view/folder-overview-context";
import { FolderTasksMain } from "./tasks-view/folder-tasks-main";
import { TaskFocusContext } from "./tasks-view/task-focus-context";
import type { TaskViewData, OverviewViewData, ViewDto, TaskItemDto } from "../../../views-type";
import { ViewType } from "@/types/view-type";
import { motion, AnimatePresence } from "framer-motion";
import { Loader2 } from "lucide-react";

interface FolderViewSwitcherProps {
  workspaceId: string;
  entityId: string;
  view: ViewDto | null;
  data: TaskViewData | OverviewViewData | any;
  viewHeader: React.ReactNode;
  isLoading?: boolean;
  isContextOpen: boolean;
  setIsContextOpen: (open: boolean) => void;
}

export function FolderViewSwitcher({
  view,
  data,
  viewHeader,
  isLoading,
  isContextOpen,
  setIsContextOpen,
}: FolderViewSwitcherProps) {
  const [selection, setSelection] = useState<{
    id: string;
    type: "Task";
    name: string;
  } | null>(null);

  // Reset selection when switching views
  useEffect(() => {
    setSelection(null);
  }, [view?.id, view?.viewType]);

  const isOverview = view?.viewType === ViewType.Overview;

  const overviewData = isOverview ? (data as OverviewViewData) : null;
  const taskData = !isOverview ? (data as TaskViewData) : null;

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
          <FolderOverviewMain 
            name={overviewData.name} 
            description={overviewData.description} 
            stats={overviewData.stats}
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
          <FolderTasksMain 
            data={taskData} 
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

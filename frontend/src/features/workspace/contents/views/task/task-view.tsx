import { EntityViewFrame } from "../entity-view-frame";

import { TaskDetailCanvas } from "./components/task-detail-canvas";

interface TaskViewProps {
  taskId: string;
}

export function TaskView({ taskId }: TaskViewProps) {
  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <div>{/* Breadcrumbs will go here */} Task Breadcrumb</div>
          <div>{/* Actions will go here */} Actions</div>
        </div>
      }
      // Task view has no subheader, just the massive canvas
    >
      <div className="h-full w-full">
        <TaskDetailCanvas taskId={taskId} />
      </div>
    </EntityViewFrame>
  );
}

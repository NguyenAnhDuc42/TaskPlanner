import { EntityViewFrame } from "../entity-view-frame";
import { TaskDetailCanvas } from "./components/task-detail-canvas";
import { useNavigate, useParams } from "@tanstack/react-router";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { MoreHorizontal, Trash2 } from "lucide-react";
import { useDeleteTaskMutation } from "../../hierarchy/hierarchy-api";

interface TaskViewProps {
  taskId: string;
}

export function TaskView({ taskId }: Readonly<TaskViewProps>) {
  const { workspaceId } = useParams({ strict: false }) as { workspaceId: string };
  const navigate = useNavigate();
  const [deleteTask] = useDeleteTaskMutation();

  const handleDelete = async () => {
    if (confirm("Are you sure you want to delete this task?")) {
      try {
        await deleteTask({ workspaceId: workspaceId || "", taskId }).unwrap();
        navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
      } catch (err) {
        console.error("Failed to delete task", err);
      }
    }
  };

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full px-2">
          <div className="text-xs font-semibold text-muted-foreground flex items-center gap-1">
            <span>Tasks</span>
            <span className="text-muted-foreground/50">/</span>
            <span className="text-foreground font-medium">Detail</span>
          </div>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={handleDelete}
                className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Delete Task
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <div className="h-full w-full">
        <TaskDetailCanvas taskId={taskId} />
      </div>
    </EntityViewFrame>
  );
}

import { observer } from "mobx-react-lite";
import { useNavigate } from "@tanstack/react-router";
import { DynamicIcon } from "@/components/dynamic-icon";
import { cn } from "@/lib/utils";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";

interface TaskListPanelProps {
  spaceId: string;
  activeTaskId: string;
}

// Flat list of the space's top-level tasks — same set the Board shows — so you can jump between
// tasks from the detail view without going back. No nesting: subtasks are already shown inline
// in the task detail canvas itself, this is purely for switching which top-level task you're on.
export const TaskListPanel = observer(function TaskListPanel({ spaceId, activeTaskId }: TaskListPanelProps) {
  const rootStore = useWorkspaceRootStore();
  const { workspaceId } = useWorkspace();
  const navigate = useNavigate();

  const tasks = rootStore.taskStore.getBySpace(spaceId).filter((t) => !t.parentTaskId);

  return (
    <div className="flex-1 overflow-y-auto px-1.5 pb-3 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
      {tasks.length === 0 && (
        <p className="text-[10px] text-muted-foreground/40 italic px-1.5 py-1">No tasks in this space.</p>
      )}
      {tasks.map((t) => {
        const isActive = t.id === activeTaskId;
        return (
          <button
            key={t.id}
            type="button"
            onClick={() => navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: t.id } })}
            className={cn(
              "w-full flex items-center gap-1.5 px-1.5 py-1 rounded-md mb-px text-left transition-colors cursor-pointer",
              isActive
                ? "bg-primary/10 text-primary"
                : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
            )}
          >
            <DynamicIcon name={t.icon || "Circle"} size={13} color={t.color || "#ffffff"} className="shrink-0" />
            <span className="text-[11px] font-semibold truncate min-w-0 flex-1">{t.name}</span>
          </button>
        );
      })}
    </div>
  );
});

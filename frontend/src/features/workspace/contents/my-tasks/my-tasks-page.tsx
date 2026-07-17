import { observer } from "mobx-react-lite";
import { useNavigate } from "@tanstack/react-router";
import { format, isBefore, startOfDay } from "date-fns";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useUser } from "@/features/auth/auth-api";
import { EntityViewFrame } from "../views/entity-view-frame";
import { DynamicIcon } from "@/components/dynamic-icon";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import { getBoardSortComparator } from "../views/space/space-board-types";
import type { BoardItem } from "../views/space/space-board-types";
import { ListTodo } from "lucide-react";
import { cn } from "@/lib/utils";

export const MyTasksPage = observer(function MyTasksPage() {
  const { workspaceId } = useWorkspace();
  const { data: currentUser } = useUser();
  const rootStore = useWorkspaceRootStore();
  const navigate = useNavigate();

  const myMember = currentUser?.id ? rootStore.memberStore.getByUserId(currentUser.id) : undefined;

  const groups = (() => {
    if (!myMember) return [];

    const assignedTaskIds = new Set(rootStore.assigneeStore.getByMember(myMember.id).map((a) => a.taskId));
    const items: BoardItem[] = [...assignedTaskIds]
      .map((id) => rootStore.taskStore.getById(id))
      .filter((t): t is NonNullable<typeof t> => !!t && !t.isArchived)
      .map((t) => ({ ...t, __type: "task" as const }));

    const bySpace = new Map<string, BoardItem[]>();
    for (const item of items) {
      const key = item.spaceId ?? "unknown";
      if (!bySpace.has(key)) bySpace.set(key, []);
      bySpace.get(key)!.push(item);
    }

    const comparator = getBoardSortComparator("priority");
    return [...bySpace.entries()]
      .map(([spaceId, tasks]) => ({
        space: rootStore.spaceStore.getById(spaceId),
        tasks: tasks.sort(comparator),
      }))
      .sort((a, b) => (a.space?.name ?? "").localeCompare(b.space?.name ?? ""));
  })();

  const handleOpenTask = (taskId: string) => {
    navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId } });
  };

  const totalCount = groups.reduce((sum, g) => sum + g.tasks.length, 0);

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center gap-1.5 text-xs">
          <ListTodo className="h-3.5 w-3.5 text-muted-foreground" />
          <span className="font-semibold text-foreground/80">My Tasks</span>
          {totalCount > 0 && (
            <span className="text-[10px] font-bold text-muted-foreground/50">{totalCount}</span>
          )}
        </div>
      }
    >
      <div className="h-full w-full overflow-y-auto px-4 py-4 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
        {totalCount === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-center gap-1.5">
            <ListTodo className="h-6 w-6 text-muted-foreground/30" />
            <p className="text-sm font-semibold text-foreground/70">No tasks assigned to you</p>
            <p className="text-xs text-muted-foreground/60 max-w-xs">
              Tasks assigned to you across every space in this workspace will show up here.
            </p>
          </div>
        ) : (
          <div className="max-w-3xl mx-auto flex flex-col gap-6">
            {groups.map(({ space, tasks }) => (
              <div key={space?.id ?? "unknown"} className="flex flex-col">
                <div className="flex items-center gap-1.5 px-1 py-1.5 border-b border-border/30">
                  <DynamicIcon name={space?.icon ?? "Orbit"} size={13} color={space?.color ?? "#ffffff"} />
                  <span className="text-[11px] font-bold text-foreground/70">{space?.name ?? "Unknown Space"}</span>
                  <span className="text-[10px] font-semibold text-muted-foreground/40">{tasks.length}</span>
                </div>
                <div className="flex flex-col divide-y divide-border/15">
                  {tasks.map((task) => (
                    <TaskListRow key={task.id} task={task} onClick={() => handleOpenTask(task.id)} />
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </EntityViewFrame>
  );
});

const TaskListRow = observer(function TaskListRow({ task, onClick }: { task: BoardItem; onClick: () => void }) {
  const rootStore = useWorkspaceRootStore();
  const status = task.statusId ? rootStore.statusStore.getById(task.statusId) : undefined;
  const isOverdue = task.dueDate ? isBefore(startOfDay(new Date(task.dueDate)), startOfDay(new Date())) : false;

  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-3 h-8 px-1 w-full text-left hover:bg-muted/30 transition-colors cursor-pointer group"
    >
      <StatusBadge status={status} variant="text" className="shrink-0 w-28" />
      <span className="flex-1 min-w-0 text-[12px] text-foreground/85 truncate group-hover:text-foreground">
        {task.name}
      </span>
      <PriorityBadge priority={task.priority} />
      {task.dueDate && (
        <span className={cn(
          "text-[9px] font-bold uppercase tracking-wide shrink-0 w-14 text-right",
          isOverdue ? "text-destructive" : "text-muted-foreground/50",
        )}>
          {format(new Date(task.dueDate), "MMM d")}
        </span>
      )}
    </button>
  );
});

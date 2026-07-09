import { useCallback, useLayoutEffect, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { TaskViewSkeleton } from "./task-view-skeleton";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { TaskAssignees } from "../task-components/task-assignees";
import { TaskComments } from "../task-components/task-comments";
import { TaskSubtasks } from "../task-components/task-subtasks";
import type { Priority } from "@/types/priority";
import type { TaskRecord } from "@/types/projects/task-record";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import type { SyncEngine } from "@/sync/sync-engine";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";
import { FileText, MessageSquare, Plus, Users, X } from "lucide-react";
import { cn } from "@/lib/utils";

// TEMP — Files rail spike, mock content only. Delete FilesPanelMock once the shape is
// confirmed and the real Attachments feature (AttachmentLink.ProjectTaskId) gets wired in.
function FilesPanelMock() {
  return (
    <div className="p-2 flex flex-col gap-1.5">
      {["brief.pdf", "mockup.png", "notes.docx"].map((name) => (
        <div
          key={name}
          className="flex items-center gap-2 p-1.5 rounded-md border border-border/40 bg-muted/20 hover:bg-muted/40 transition-colors cursor-pointer"
        >
          <FileText className="h-3.5 w-3.5 text-muted-foreground/60 shrink-0" />
          <span className="text-[10px] text-muted-foreground truncate">{name}</span>
        </div>
      ))}
      <button
        type="button"
        className="flex items-center justify-center gap-1 p-1.5 rounded-md border border-dashed border-border/40 text-muted-foreground/50 hover:text-muted-foreground hover:border-border transition-colors"
      >
        <Plus className="h-3.5 w-3.5" />
        <span className="text-[10px]">Upload</span>
      </button>
    </div>
  );
}

interface TaskDetailCanvasProps {
  taskId?: string;
}

function useDebouncedTaskUpdate(taskMutations: TaskMutations, syncEngine: SyncEngine, taskId: string) {
  const taskIdRef = useRef(taskId);
  useLayoutEffect(() => { taskIdRef.current = taskId; });
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  return useCallback((patches: Partial<TaskRecord>) => {
    taskMutations.updateLocal(taskIdRef.current, patches).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);
}

export const TaskDetailCanvas = observer(function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready, error } = useSyncReady();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const task = taskId ? rootStore.taskStore.getById(taskId) : undefined;
  const updateTask = useDebouncedTaskUpdate(taskMutations, syncEngine, taskId || "");
  const { canCreateContent: canEdit } = useWorkspaceRole();
  const [railPanel, setRailPanel] = useState<"files" | "comments" | "assignees" | null>(null); // TEMP — rail spike

  if (!taskId) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground text-sm italic">
        No task selected.
      </div>
    );
  }

  if (!task) {
    if (error) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Failed to load task</span>
          <span className="text-xs text-muted-foreground">The task may have been deleted, or there was a server error.</span>
        </div>
      );
    }

    if (ready) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Task Not Found</span>
          <span className="text-xs text-muted-foreground">The task may have been deleted by another user.</span>
        </div>
      );
    }

    return <TaskViewSkeleton />;
  }

  const handleStatusChange = (statusId: string | null) => {
    updateTask({ statusId });
  };

  const handlePriorityChange = (priority: Priority) => {
    updateTask({ priority });
  };

  const handleStartDateChange = (date: Date | undefined) => {
    updateTask({ startDate: date ? date.toISOString() : null });
  };

  const handleDueDateChange = (date: Date | undefined) => {
    updateTask({ dueDate: date ? date.toISOString() : null });
  };

  const handleClearDates = () => {
    updateTask({ startDate: null, dueDate: null });
  };

  const isSubtask = !!task.parentTaskId;

  const railTabs: { key: "files" | "comments" | "assignees"; Icon: typeof FileText; label: string }[] = [
    { key: "files", Icon: FileText, label: "Files" },
    { key: "comments", Icon: MessageSquare, label: "Comments" },
    { key: "assignees", Icon: Users, label: "Assignees" },
  ];

  return (
    <div className="relative flex h-full w-full bg-card overflow-hidden">
      <div className={cn("flex-1 flex flex-col overflow-hidden min-w-0", !isSubtask && "pr-8")}>
      {/* Task Content Scroll Area */}
      <div className="flex-1 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        <div className="w-full p-4 md:p-8 space-y-6">
          {/* Header Title Area */}
          <div className="flex items-start gap-3">
            <UniversalPicker
              icon={task.icon || "CheckSquare"}
              color={task.color || "#6366f1"}
              onSelect={(icon, color) => updateTask({ icon, color })}
              size="lg"
            />

            <DebouncedInput
              value={task.name || ""}
              onChange={(val) => {
                if (val.trim() && val !== task.name) {
                  updateTask({ name: val.trim() });
                }
              }}
              debounceMs={1500}
              placeholder="Untitled Task"
              className="text-2xl font-black text-foreground border-none p-0 focus-visible:ring-0 bg-transparent h-auto outline-none w-full"
            />
          </div>

          {/* Properties Area */}
          <div className="flex flex-col gap-3.5 pb-1 border-b border-border/30">
            {/* Row 1: Task State and Timing (Status, Priority, Dates) */}
            <div className="flex flex-wrap items-center gap-2.5">
              <StatusSelect
                value={task.statusId ?? undefined}
                onChange={handleStatusChange}
                spaceId={task.spaceId!}
              />

              <PrioritySelect
                value={task.priority}
                onChange={handlePriorityChange}
                trigger={
                  <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                    <PriorityBadge priority={task.priority} />
                  </button>
                }
              />

              <DateSelect
                startDate={task.startDate}
                dueDate={task.dueDate}
                onStartDateChange={handleStartDateChange}
                onDueDateChange={handleDueDateChange}
                onClearDates={handleClearDates}
                size="sm"
              />
            </div>
          </div>

          {/* Document Section */}
          {task.defaultDocumentId && (
            <BlockEditor key={task.defaultDocumentId} documentId={task.defaultDocumentId} editable={canEdit} />
          )}

          {/* Subtasks Section — back inline at task detail, not in the rail (only top-level
              tasks have subtasks; a subtask can't have its own subtasks) */}
          {!isSubtask && <TaskSubtasks taskId={taskId} />}

          {/* Subtasks stay simple — Comments inline, no rail (see isSubtask comment above) */}
          {isSubtask && <TaskComments taskId={taskId} />}
        </div>
      </div>
      </div>

      {!isSubtask && (
        <>
          {/* Floating overlay, not a layout sibling — the main content always keeps its full
              width. Fixed side-by-side columns starved the content down to nothing when this
              renders inside the narrower context-panel column (opened from Space board etc). */}
          {railPanel && (
            <div className="absolute top-0 right-8 h-full w-72 max-w-[calc(100%-2rem)] border-l border-border/40 bg-card shadow-xl flex flex-col overflow-hidden z-20">
              <div className="flex items-center justify-between px-2 py-1.5 border-b border-border/40 shrink-0">
                <span className="text-[10px] font-bold uppercase tracking-wide text-muted-foreground">{railPanel}</span>
                <button type="button" onClick={() => setRailPanel(null)} className="text-muted-foreground hover:text-foreground">
                  <X className="h-3.5 w-3.5" />
                </button>
              </div>
              <div className="flex-1 overflow-y-auto p-2">
                {railPanel === "files" && <FilesPanelMock />}
                {railPanel === "comments" && <TaskComments taskId={taskId} hideHeading />}
                {railPanel === "assignees" && <TaskAssignees taskId={taskId} />}
              </div>
            </div>
          )}
          <div className="absolute top-0 right-0 h-full w-8 border-l border-border/40 bg-card flex flex-col items-center py-2 gap-2 z-10">
            {railTabs.map(({ key, Icon, label }) => (
              <button
                key={key}
                type="button"
                onClick={() => setRailPanel((p) => (p === key ? null : key))}
                className={cn(
                  "p-1.5 rounded transition-colors",
                  railPanel === key ? "bg-primary/10 text-primary" : "text-muted-foreground/50 hover:text-muted-foreground"
                )}
                title={label}
              >
                <Icon className="h-4 w-4" />
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
});

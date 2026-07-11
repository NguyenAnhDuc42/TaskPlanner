import { useEffect, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { CheckSquare } from "lucide-react";
import { toast } from "sonner";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
import { extractErrorMessage } from "@/types/api-error";
import { EntityLayerType } from "@/types/entity-layer-type";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { TaskNodeItem } from "./task-node-item";
import { Priority } from "@/types/priority";

interface NodeTasksListProps {
  nodeId: string;
  parentType: EntityLayerType;
  isExpanded: boolean;
  spaceId: string;
  isCreating?: boolean;
  onCreatingChange?: (creating: boolean) => void;
}

export const NodeTasksList = observer(function NodeTasksList({
  nodeId,
  parentType,
  isExpanded,
  spaceId,
  isCreating = false,
  onCreatingChange,
}: NodeTasksListProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [newName, setNewName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    if (!isCreating) return;
    // The row can mount while its parent Collapsible is still animating open (triggered from a
    // collapsed space/folder) — a synchronous focus() call right after mount can land before the
    // input is actually focusable yet. Double rAF waits for that layout/paint to settle first.
    let raf2 = 0;
    const raf1 = requestAnimationFrame(() => {
      raf2 = requestAnimationFrame(() => inputRef.current?.focus());
    });
    return () => { cancelAnimationFrame(raf1); cancelAnimationFrame(raf2); };
  }, [isCreating]);

  if (!isExpanded) return null;

  const isFolder = parentType === EntityLayerType.ProjectFolder;
  const tasks = rootStore.taskStore.all
    .filter((t) => !t.parentTaskId && (isFolder ? t.folderId === nodeId : (t.spaceId === nodeId && !t.folderId)))
    .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const handleCreate = () => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const name = newName.trim();
    onCreatingChange?.(false);
    setNewName("");
    if (name) {
      taskMutations.create({
        name,
        priority: Priority.None,
        spaceId,
        folderId: isFolder ? nodeId : null,
      }).catch((err) => toast.error(extractErrorMessage(err, "Failed to create task")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  };

  return (
    <SortableContext
      items={tasks.map((t) => `task-${t.id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {tasks.map((t) => (
          <TaskNodeItem
            key={t.id}
            taskId={t.id}
            parentId={nodeId}
            parentType={parentType}
            spaceId={spaceId}
          />
        ))}

        {isCreating && (
          <div className="flex items-center gap-1.5 px-1 py-0.5 rounded-md border border-primary/40 bg-primary/5 mb-px">
            <div className="w-5 h-5 flex items-center justify-center shrink-0">
              <CheckSquare className="h-3.5 w-3.5 opacity-60" />
            </div>
            <input
              ref={inputRef}
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
                if (e.key === "Escape") { onCreatingChange?.(false); setNewName(""); }
              }}
              onBlur={handleCreate}
              placeholder="Task name..."
              className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none text-foreground placeholder:text-muted-foreground/40"
            />
          </div>
        )}
      </div>
    </SortableContext>
  );
});

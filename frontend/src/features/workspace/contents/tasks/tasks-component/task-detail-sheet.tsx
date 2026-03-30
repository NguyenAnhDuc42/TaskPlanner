import { type ReactNode, useMemo, useState } from "react";
import { format } from "date-fns";
import { Calendar, Flag, Trash2, User } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Priority } from "@/types/priority";
import type { TaskDto } from "../tasks-type";
import {
  useUpdateTask,
  useDeleteTask,
  useTaskAssigneeCandidates,
  useTaskAssignees,
} from "../tasks-api";
import { useStatuses } from "../../hierarchy/statuses-api";

interface TaskDetailSheetProps {
  task: TaskDto | null;
  workspaceId: string;
  isOpen: boolean;
  onClose: () => void;
}

const toInputDate = (value?: string) => (value ? value.slice(0, 10) : "");
const toIsoDateStart = (value: string) =>
  value ? new Date(`${value}T00:00:00`).toISOString() : undefined;

export function TaskDetailSheet({
  task,
  workspaceId,
  isOpen,
  onClose,
}: TaskDetailSheetProps) {
  if (!task) return null;

  return (
    <TaskDetailSheetBody
      key={task.id}
      task={task}
      workspaceId={workspaceId}
      isOpen={isOpen}
      onClose={onClose}
    />
  );
}

function TaskDetailSheetBody({
  task,
  workspaceId,
  isOpen,
  onClose,
}: TaskDetailSheetProps & { task: TaskDto }) {
  const initialName = task.name;
  const initialDescription = task.description ?? "";
  const initialPriority = task.priority;
  const initialStartDate = toInputDate(task.startDate);
  const initialDueDate = toInputDate(task.dueDate);
  const initialStatusId = task.statusId ?? "";
  const initialAssigneeIds = task.assignees.map((a) => a.id);

  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);
  const [priority, setPriority] = useState<Priority>(initialPriority);
  const [startDate, setStartDate] = useState(initialStartDate);
  const [dueDate, setDueDate] = useState(initialDueDate);
  const [statusId, setStatusId] = useState(initialStatusId);
  const [selectedAssigneeIds, setSelectedAssigneeIds] =
    useState<string[]>(initialAssigneeIds);
  const [assigneeSearch, setAssigneeSearch] = useState("");
  const [isDeleteConfirming, setIsDeleteConfirming] = useState(false);

  const updateTask = useUpdateTask(workspaceId);
  const deleteTask = useDeleteTask(workspaceId);
  const { data: statuses } = useStatuses(task.projectListId, "ProjectList");
  const { data: assignedMembers } = useTaskAssignees(
    workspaceId,
    task.id,
    isOpen,
  );
  const { data: candidateMembers } = useTaskAssigneeCandidates(
    workspaceId,
    task.id,
    assigneeSearch,
    isOpen,
  );

  const effectiveStatusId =
    (statusId && statuses?.some((s) => s.id === statusId)
      ? statusId
      : statuses?.find((s) => s.isDefault)?.id || statuses?.[0]?.id) ?? "";

  const hasInvalidDateRange =
    !!startDate && !!dueDate && new Date(startDate) > new Date(dueDate);

  const isDirty = useMemo(() => {
    const sortedCurrentAssignees = [...selectedAssigneeIds].sort();
    const sortedInitialAssignees = [...initialAssigneeIds].sort();

    return (
      name !== initialName ||
      description !== initialDescription ||
      priority !== initialPriority ||
      startDate !== initialStartDate ||
      dueDate !== initialDueDate ||
      effectiveStatusId !== initialStatusId ||
      sortedCurrentAssignees.join(",") !== sortedInitialAssignees.join(",")
    );
  }, [
    name,
    initialName,
    description,
    initialDescription,
    priority,
    initialPriority,
    startDate,
    initialStartDate,
    dueDate,
    initialDueDate,
    effectiveStatusId,
    initialStatusId,
    selectedAssigneeIds,
    initialAssigneeIds,
  ]);

  const isPending = updateTask.isPending || deleteTask.isPending;
  const memberByUserId = useMemo(() => {
    const map = new Map<string, { userId: string; userName: string }>();
    (assignedMembers ?? []).forEach((m) =>
      map.set(m.userId, { userId: m.userId, userName: m.userName }),
    );
    (candidateMembers ?? []).forEach((m) =>
      map.set(m.userId, { userId: m.userId, userName: m.userName }),
    );
    task.assignees.forEach((a) =>
      map.set(a.id, { userId: a.id, userName: a.name }),
    );
    return map;
  }, [assignedMembers, candidateMembers, task.assignees]);

  const selectedAssignees = selectedAssigneeIds
    .map((id) => memberByUserId.get(id))
    .filter((member): member is { userId: string; userName: string } => !!member);

  const availableCandidates = (candidateMembers ?? []).filter(
    (candidate) => !selectedAssigneeIds.includes(candidate.userId),
  );

  const toggleAssignee = (userId: string) => {
    setSelectedAssigneeIds((prev) =>
      prev.includes(userId)
        ? prev.filter((id) => id !== userId)
        : [...prev, userId],
    );
  };

  const handleSave = async () => {
    if (!name.trim() || hasInvalidDateRange || isPending) return;

    try {
      await updateTask.mutateAsync({
        taskId: task.id,
        name: name.trim(),
        description: description.trim() || undefined,
        statusId: effectiveStatusId || undefined,
        priority,
        startDate: toIsoDateStart(startDate),
        dueDate: toIsoDateStart(dueDate),
        assigneeIds: selectedAssigneeIds,
      });
      toast.success("Task updated");
      setIsDeleteConfirming(false);
    } catch (error) {
      console.error(error);
      toast.error("Failed to update task");
    }
  };

  const handleDelete = async () => {
    try {
      await deleteTask.mutateAsync(task.id);
      toast.success("Task deleted");
      onClose();
    } catch (error) {
      console.error(error);
      toast.error("Failed to delete task");
    }
  };

  return (
    <Sheet open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="sm:max-w-xl w-[90%] p-0 bg-background/95 backdrop-blur-xl border-l border-primary/10">
        <div className="flex flex-col h-full">
          <div className="p-6 pb-4 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Flag className="h-4 w-4 text-muted-foreground" />
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-widest">
                  Task Detail
                </span>
              </div>
              <Badge
                variant="secondary"
                className="bg-primary/5 text-primary border-primary/10 hover:bg-primary/10"
              >
                {statuses?.find((s) => s.id === effectiveStatusId)?.name ||
                  "No Status"}
              </Badge>
            </div>

            <SheetHeader>
              <SheetTitle className="space-y-2">
                <Input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="text-lg font-black"
                  disabled={isPending}
                />
              </SheetTitle>
            </SheetHeader>
          </div>

          <Separator className="bg-primary/5" />

          <div className="grid grid-cols-2 gap-px bg-primary/5 p-px">
            <AttributeEditor
              icon={<Flag className="h-3.5 w-3.5" />}
              label="Priority"
            >
              <select
                className="h-8 rounded-md border bg-background px-2 text-xs w-full"
                value={priority}
                onChange={(e) => setPriority(e.target.value as Priority)}
                disabled={isPending}
              >
                <option value={Priority.Low}>Low</option>
                <option value={Priority.Normal}>Normal</option>
                <option value={Priority.High}>High</option>
                <option value={Priority.Urgent}>Urgent</option>
              </select>
            </AttributeEditor>

            <AttributeEditor
              icon={<User className="h-3.5 w-3.5" />}
              label="Status"
            >
              <select
                className="h-8 rounded-md border bg-background px-2 text-xs w-full"
                value={effectiveStatusId}
                onChange={(e) => setStatusId(e.target.value)}
                disabled={isPending || !statuses?.length}
              >
                {(statuses ?? []).map((status) => (
                  <option key={status.id} value={status.id}>
                    {status.name}
                  </option>
                ))}
              </select>
            </AttributeEditor>

            <AttributeEditor
              icon={<Calendar className="h-3.5 w-3.5" />}
              label="Start Date"
            >
              <Input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="h-8 text-xs"
                disabled={isPending}
              />
            </AttributeEditor>

            <AttributeEditor
              icon={<Calendar className="h-3.5 w-3.5" />}
              label="Due Date"
            >
              <Input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="h-8 text-xs"
                disabled={isPending}
              />
            </AttributeEditor>
          </div>

          <Separator className="bg-primary/5" />

          <div className="flex-1 p-6 space-y-4 overflow-y-auto">
            <div className="space-y-2">
              <h4 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                Description
              </h4>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="text-sm"
                placeholder="Description"
                disabled={isPending}
              />
            </div>

            <div className="space-y-3 pt-2">
              <h4 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                Assigned
              </h4>
              <div className="flex flex-wrap gap-1.5">
                {selectedAssignees.map((member) => (
                  <button
                    key={member.userId}
                    type="button"
                    onClick={() => toggleAssignee(member.userId)}
                    className="h-7 px-2 rounded-md border text-[11px] bg-primary/10 border-primary/40 text-primary"
                    disabled={isPending}
                  >
                    {member.userName} x
                  </button>
                ))}
                {selectedAssignees.length === 0 && (
                  <div className="text-xs text-muted-foreground">No assignees</div>
                )}
              </div>
            </div>

            <div className="space-y-3 pt-2">
              <h4 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                Add Assignee
              </h4>
              <Input
                value={assigneeSearch}
                onChange={(e) => setAssigneeSearch(e.target.value)}
                placeholder="Search accessible members..."
                className="h-8 text-xs"
                disabled={isPending}
              />
              <div className="flex flex-wrap gap-1.5">
                {availableCandidates.map((member) => (
                  <button
                    key={member.userId}
                    type="button"
                    onClick={() => toggleAssignee(member.userId)}
                    className="h-7 px-2 rounded-md border text-[11px] bg-background border-border text-muted-foreground"
                    disabled={isPending}
                  >
                    {member.userName}
                  </button>
                ))}
                {availableCandidates.length === 0 && (
                  <div className="text-xs text-muted-foreground">
                    No available candidates
                  </div>
                )}
              </div>
            </div>

            {hasInvalidDateRange && (
              <div className="text-xs text-destructive">
                Start date cannot be later than due date.
              </div>
            )}
          </div>

          <div className="p-4 bg-muted/5 border-t border-primary/5 flex items-center justify-between gap-2">
            <div className="text-[10px] text-muted-foreground font-medium italic">
              Created on {format(new Date(task.createdAt), "MMMM dd, yyyy")}
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant={isDeleteConfirming ? "destructive" : "outline"}
                size="sm"
                onClick={() => {
                  if (isDeleteConfirming) {
                    void handleDelete();
                    return;
                  }
                  setIsDeleteConfirming(true);
                }}
                disabled={isPending}
              >
                <Trash2 className="h-3.5 w-3.5 mr-1" />
                {isDeleteConfirming ? "Confirm Delete" : "Delete"}
              </Button>
              <Button
                type="button"
                size="sm"
                onClick={() => void handleSave()}
                disabled={!isDirty || !name.trim() || hasInvalidDateRange || isPending}
              >
                Save
              </Button>
            </div>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}

function AttributeEditor({
  icon,
  label,
  children,
}: {
  icon: ReactNode;
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="bg-background p-3 flex flex-col gap-1.5">
      <div className="flex items-center gap-2 text-muted-foreground/60">
        {icon}
        <span className="text-[10px] font-black uppercase tracking-widest">
          {label}
        </span>
      </div>
      {children}
    </div>
  );
}

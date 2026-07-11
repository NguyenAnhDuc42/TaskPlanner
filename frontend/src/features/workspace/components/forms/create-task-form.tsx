import { useReducer, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { extractErrorMessage } from "@/types/api-error";
import { Button } from "@/components/ui/button";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Priority } from "@/types/priority";
import { Circle, User, Check } from "lucide-react";
import { toast } from "sonner";
import {
  AttributeButton,
  IconColorPicker,
} from "./form-elements";
import { DateSelect } from "@/components/date-select";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { UserAvatar } from "@/components/user-avatar";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
import { AssigneeMutations } from "@/mutations/assignee.mutations";
import type { Status } from "@/types/status";

interface CreateTaskFormProps {
  readonly parentId: string;
  readonly parentType: EntityLayerType;
  readonly defaultStatusId?: string;
  readonly onSuccess?: (taskId: string) => void;
  readonly onCancel?: () => void;
}

interface TaskFormState {
  readonly name: string;
  readonly priority: Priority;
  readonly icon: string;
  readonly color: string;
  readonly selectedStatusId: string | null | undefined;
  readonly startDate: Date | undefined;
  readonly dueDate: Date | undefined;
  readonly selectedAssigneeIds: readonly string[];
}

type TaskFormAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_PRIORITY"; readonly payload: Priority }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string }
  | { readonly type: "SET_STATUS"; readonly payload: string | null | undefined }
  | { readonly type: "SET_START_DATE"; readonly payload: Date | undefined }
  | { readonly type: "SET_DUE_DATE"; readonly payload: Date | undefined }
  | { readonly type: "TOGGLE_ASSIGNEE"; readonly payload: string }
  | { readonly type: "RESET"; readonly payload?: string };

function createTaskFormReducer(defaultStatusId?: string) {
  return function (state: TaskFormState, action: TaskFormAction): TaskFormState {
    switch (action.type) {
      case "SET_NAME":
        return { ...state, name: action.payload };
      case "SET_PRIORITY":
        return { ...state, priority: action.payload };
      case "SET_ICON":
        return { ...state, icon: action.payload };
      case "SET_COLOR":
        return { ...state, color: action.payload };
      case "SET_STATUS":
        return { ...state, selectedStatusId: action.payload };
      case "SET_START_DATE":
        return { ...state, startDate: action.payload };
      case "SET_DUE_DATE":
        return { ...state, dueDate: action.payload };
      case "TOGGLE_ASSIGNEE":
        return {
          ...state,
          selectedAssigneeIds: state.selectedAssigneeIds.includes(action.payload)
            ? state.selectedAssigneeIds.filter((id) => id !== action.payload)
            : [...state.selectedAssigneeIds, action.payload],
        };
      case "RESET":
        return {
          name: "",
          priority: Priority.Normal,
          icon: "Circle",
          color: "#94a3b8",
          selectedStatusId: action.payload || defaultStatusId,
          startDate: undefined,
          dueDate: undefined,
          selectedAssigneeIds: [],
        };
      default:
        return state;
    }
  };
}

export const CreateTaskForm = observer(function CreateTaskForm({
  parentId,
  parentType,
  defaultStatusId,
  onSuccess,
  onCancel,
}: Readonly<CreateTaskFormProps>) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const assigneeMutations = useMemo(() => new AssigneeMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isCreating, setIsCreating] = useReducer((_: boolean, v: boolean) => v, false);
  const [assigneeSearch, setAssigneeSearch] = useState("");
  const [state, dispatch] = useReducer(
    createTaskFormReducer(defaultStatusId),
    {
      name: "",
      priority: Priority.Normal,
      icon: "Circle",
      color: "#94a3b8",
      selectedStatusId: defaultStatusId,
      startDate: undefined,
      dueDate: undefined,
      selectedAssigneeIds: [],
    }
  );
  const nameInputRef = useRef<HTMLTextAreaElement>(null);

  const allMembers = rootStore.memberStore.all;
  const filteredMembers = allMembers.filter(
    (m) =>
      m.name.toLowerCase().includes(assigneeSearch.toLowerCase()) ||
      m.email?.toLowerCase().includes(assigneeSearch.toLowerCase()),
  );
  const folder = parentType === "ProjectFolder" ? rootStore.folderStore.getById(parentId) : undefined;
  const spaceId = parentType === "ProjectSpace" ? parentId : folder?.spaceId;
  const statuses = (spaceId ? rootStore.statusStore.getVisibleForSpace(spaceId) : rootStore.statusStore.all) as Status[];

  const onSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!state.name.trim()) return;

    setIsCreating(true);
    try {
      const record = await taskMutations.create({
        name: state.name,
        priority: state.priority,
        icon: state.icon,
        color: state.color,
        statusId: state.selectedStatusId,
        startDate: state.startDate?.toISOString(),
        dueDate: state.dueDate?.toISOString(),
        spaceId: spaceId ?? null,
        folderId: parentType === "ProjectFolder" ? parentId : null,
      });

      if (state.selectedAssigneeIds.length > 0) {
        await Promise.all(
          state.selectedAssigneeIds.map((memberId) =>
            assigneeMutations.create(record.id, memberId).catch((err) =>
              console.error("Failed to assign member to new task", err),
            ),
          ),
        );
      }

      toast.success("Task created");

      // Stays open — reset for the next one instead of closing.
      dispatch({ type: "RESET" });
      setAssigneeSearch("");
      nameInputRef.current?.focus();

      onSuccess?.(record.id);
    } catch (error) {
      console.error(error);
      toast.error(extractErrorMessage(error, "Failed to create task"));
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-2 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker
            icon={state.icon}
            color={state.color}
            onChange={(i, c) => {
              dispatch({ type: "SET_ICON", payload: i });
              dispatch({ type: "SET_COLOR", payload: c });
            }}
          />
          <textarea
            ref={nameInputRef}
            placeholder="Task title"
            aria-label="Task title"
            value={state.name}
            onChange={(e) => dispatch({ type: "SET_NAME", payload: e.target.value })}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none resize-none min-h-5.5"
            rows={1}
            onKeyDown={(e) => {
              if (e.key === " ") {
                e.stopPropagation();
              }
              if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
                e.preventDefault();
                e.stopPropagation();
                onSubmit();
              }
            }}
          />
        </div>
      </div>

      {/* Attribute Strip */}
      <div className="px-3 py-1.5 flex flex-nowrap items-center gap-1.5 border-t border-border/5 overflow-x-auto [&::-webkit-scrollbar]:hidden">
        <StatusSelect
          value={state.selectedStatusId || undefined}
          onChange={(statusId) => dispatch({ type: "SET_STATUS", payload: statusId })}
          spaceId={spaceId}
          align="start"
          trigger={
            <AttributeButton icon={state.selectedStatusId ? undefined : Circle}>
              {state.selectedStatusId ? (
                <StatusBadge
                  status={statuses.find((s: Status) => s.id?.toLowerCase() === state.selectedStatusId?.toLowerCase())}
                />
              ) : (
                "Status"
              )}
            </AttributeButton>
          }
        />

        <PrioritySelect
          value={state.priority}
          onChange={(p) => dispatch({ type: "SET_PRIORITY", payload: p })}
          align="start"
          trigger={
            <AttributeButton>
              <PriorityBadge priority={state.priority} />
            </AttributeButton>
          }
        />

        <DateSelect
          startDate={state.startDate?.toISOString()}
          dueDate={state.dueDate?.toISOString()}
          onStartDateChange={(date) => dispatch({ type: "SET_START_DATE", payload: date })}
          onDueDateChange={(date) => dispatch({ type: "SET_DUE_DATE", payload: date })}
          align="start"
          size="sm"
          triggerClassName="h-6 px-2 text-[10px] font-medium rounded-md border border-transparent bg-muted/30 hover:bg-muted/50 text-muted-foreground transition-all cursor-pointer"
        />

        <Popover onOpenChange={(open) => { if (!open) setAssigneeSearch(""); }}>
          <PopoverTrigger asChild>
            <AttributeButton icon={User} className="ml-auto" active={state.selectedAssigneeIds.length > 0}>
              {state.selectedAssigneeIds.length > 0 ? `Assignee (${state.selectedAssigneeIds.length})` : "Assignee"}
            </AttributeButton>
          </PopoverTrigger>
          <PopoverContent
            align="end"
            className="w-52 p-0 gap-0 rounded-md border border-border shadow-md bg-background text-popover-foreground overflow-hidden"
          >
            <div className="p-0 border-b border-border">
              <input
                autoFocus
                placeholder="Filter members..."
                value={assigneeSearch}
                onChange={(e) => setAssigneeSearch(e.target.value)}
                className="h-8 w-full text-[11px] bg-transparent border-0 focus-visible:outline-none px-2"
              />
            </div>
            <div className="max-h-50 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
              {filteredMembers.length === 0 ? (
                <p className="text-[11px] text-muted-foreground/50 text-center py-3">No members found</p>
              ) : (
                filteredMembers.map((member) => {
                  const isAssigned = state.selectedAssigneeIds.includes(member.id);
                  return (
                    <button
                      key={member.id}
                      type="button"
                      onClick={() => dispatch({ type: "TOGGLE_ASSIGNEE", payload: member.id })}
                      className={`w-full flex items-center gap-2 px-2 py-1.5 text-[11px] font-semibold text-left transition-colors cursor-default outline-none select-none ${
                        isAssigned ? "bg-muted text-foreground" : "hover:bg-accent hover:text-accent-foreground"
                      }`}
                    >
                      <UserAvatar
                        name={member.name}
                        avatarUrl={member.avatarUrl}
                        className="h-5 w-5 rounded-sm shrink-0"
                        fallbackClassName="text-[8px] rounded-sm"
                      />
                      <span className="truncate font-medium flex-1">{member.name}</span>
                      {isAssigned && <Check className="h-3.5 w-3.5 text-primary shrink-0" />}
                    </button>
                  );
                })
              )}
            </div>
          </PopoverContent>
        </Popover>
      </div>

      {/* Footer Actions */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-end border-t border-border/10">
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onCancel}
            className="h-7 px-2.5 text-[10px] font-medium text-muted-foreground hover:text-foreground"
          >
            Done
          </Button>
          <Button
            type="submit"
            size="sm"
            disabled={!state.name.trim() || isCreating}
            className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
          >
            {isCreating ? "Creating..." : "Create Task"}
          </Button>
        </div>
      </div>
    </form>
  );
});

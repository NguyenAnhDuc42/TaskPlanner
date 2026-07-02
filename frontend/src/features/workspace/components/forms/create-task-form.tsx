import { useReducer, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { extractErrorMessage } from "@/types/api-error";
import { Button } from "@/components/ui/button";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Priority } from "@/types/priority";
import { Circle, User } from "lucide-react";
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
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
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
  readonly selectedStatusId: string | undefined;
  readonly startDate: Date | undefined;
  readonly dueDate: Date | undefined;
}

type TaskFormAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_PRIORITY"; readonly payload: Priority }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string }
  | { readonly type: "SET_STATUS"; readonly payload: string | undefined }
  | { readonly type: "SET_START_DATE"; readonly payload: Date | undefined }
  | { readonly type: "SET_DUE_DATE"; readonly payload: Date | undefined }
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
      case "RESET":
        return {
          name: "",
          priority: Priority.Normal,
          icon: "Circle",
          color: "#94a3b8",
          selectedStatusId: action.payload || defaultStatusId,
          startDate: undefined,
          dueDate: undefined,
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
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isCreating, setIsCreating] = useReducer((_: boolean, v: boolean) => v, false);
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
    }
  );

  // Dynamically resolve space ID depending on folder/space parentType. Task/Folder/Status are
  // all fully Bootstrap+Delta covered, so this reads straight from MobX — no lazy fetch needed.
  const folder = parentType === "ProjectFolder" ? rootStore.folderStore.getById(parentId) : undefined;
  const spaceId = parentType === "ProjectSpace" ? parentId : folder?.spaceId;

  // Derive statuses from the space (all tasks in a folder now use space workflow)
  const statuses = spaceId
    ? rootStore.statusStore.getBySpace(spaceId) as Status[]
    : [];

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
      toast.success("Task created");
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

        <AttributeButton icon={User} className="ml-auto">
          Assignee
        </AttributeButton>
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
            Cancel
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

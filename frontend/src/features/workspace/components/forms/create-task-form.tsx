import { useReducer, useMemo } from "react";
import { useCreateTaskMutation } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
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

import { useSelector } from "react-redux";

import { useGetSpaceDetailQuery, useGetSpaceItemsQuery } from "../../contents/views/space/space-api";
import { useGetFolderDetailQuery } from "../../contents/views/folder/folder-api";
import { statusSelectors } from "@/store/entityStore";
import type { Status } from "@/types/status";
import type { RootState } from "@/store";

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

export function CreateTaskForm({
  parentId,
  parentType,
  defaultStatusId,
  onSuccess,
  onCancel,
}: Readonly<CreateTaskFormProps>) {
  const { workspaceId } = useWorkspace();
  const [createTaskMutation, { isLoading: isCreating }] = useCreateTaskMutation();
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

  // Dynamically resolve space ID depending on folder/space parentType
  const folder = useSelector((state: RootState) => state.folders.entities[parentId]);
  const spaceId = parentType === "ProjectSpace" ? parentId : folder?.spaceId;

  // Lazy-load folder detail (populates workflowId + statuses into Redux) when parent is a folder
  useGetFolderDetailQuery(parentId, { skip: parentType !== "ProjectFolder" });

  // Lazy-load space detail + items (populates space statuses into Redux)
  const { data: space } = useGetSpaceDetailQuery(spaceId || "", { skip: !spaceId });
  useGetSpaceItemsQuery(spaceId || "", { skip: !spaceId });

  // Resolve the correct workflowId:
  // - Folder parent: use the folder's resolved workflowId (folder-own or inherited), stored after getFolderDetail fires
  // - Space parent: use the space's workflowId directly
  const targetWorkflowId =
    parentType === "ProjectFolder"
      ? (folder?.workflowId || space?.workflowId)
      : space?.workflowId;

  // Derive statuses from the RESOLVED workflow
  const allStatuses = useSelector((state: RootState) => statusSelectors.selectAll(state));
  const statuses = useMemo(() => {
    return targetWorkflowId
      ? allStatuses.filter((s: Status) => s.workflowId?.toLowerCase() === targetWorkflowId.toLowerCase())
      : [];
  }, [allStatuses, targetWorkflowId]);

  const onSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!state.name.trim()) return;

    try {
      const { id: newId } = await createTaskMutation({
        workspaceId: workspaceId || "",
        body: {
          parentId,
          parentType,
          name: state.name,
          priority: state.priority.toString(),
          icon: state.icon,
          color: state.color,
          statusId: state.selectedStatusId,
          startDate: state.startDate?.toISOString(),
          dueDate: state.dueDate?.toISOString(),
        },
      }).unwrap();
      toast.success("Task created");
      onSuccess?.(newId);
    } catch (error) {
      console.error(error);
      toast.error("Failed to create task");
    }
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col w-full bg-background border border-border/30 rounded-md overflow-hidden text-foreground">
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
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none resize-none min-h-[22px]"
            rows={1}
            onKeyDown={(e) => {
              if (e.key === " ") {
                e.stopPropagation();
              }
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
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
          workflowId={targetWorkflowId}
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
}

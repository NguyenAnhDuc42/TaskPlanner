import { useReducer } from "react";
import { useCreateFolderMutation } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import {
  IconColorPicker,
  AttributeButton,
} from "./form-elements";
import { DateSelect } from "@/components/date-select";
import * as Icons from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { StatusSelect } from "@/components/status-select";
import { useGetSpaceDetailQuery, useGetSpaceItemsQuery, useSpaceStatuses } from "../../contents/views/space/space-api";
import type { Status } from "@/types/status";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface CreateFolderFormProps {
  readonly spaceId: string;
  readonly onSuccess?: (id: string) => void;
  readonly onCancel?: () => void;
}

interface FolderFormState {
  readonly name: string;
  readonly icon: string;
  readonly color: string;
  readonly selectedStatusId: string | null;
  readonly priority: Priority;
  readonly startDate: Date | undefined;
  readonly dueDate: Date | undefined;
}

type FolderFormAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string }
  | { readonly type: "SET_STATUS"; readonly payload: string | null }
  | { readonly type: "SET_PRIORITY"; readonly payload: Priority }
  | { readonly type: "SET_START_DATE"; readonly payload: Date | undefined }
  | { readonly type: "SET_DUE_DATE"; readonly payload: Date | undefined }
  | { readonly type: "RESET" };

const initialFolderState: FolderFormState = {
  name: "",
  icon: "Folder",
  color: "#6366f1",
  selectedStatusId: null,
  priority: Priority.Normal,
  startDate: undefined,
  dueDate: undefined,
};

function folderFormReducer(state: FolderFormState, action: FolderFormAction): FolderFormState {
  switch (action.type) {
    case "SET_NAME":
      return { ...state, name: action.payload };
    case "SET_ICON":
      return { ...state, icon: action.payload };
    case "SET_COLOR":
      return { ...state, color: action.payload };
    case "SET_STATUS":
      return { ...state, selectedStatusId: action.payload };
    case "SET_PRIORITY":
      return { ...state, priority: action.payload };
    case "SET_START_DATE":
      return { ...state, startDate: action.payload };
    case "SET_DUE_DATE":
      return { ...state, dueDate: action.payload };
    case "RESET":
      return initialFolderState;
    default:
      return state;
  }
}

export function CreateFolderForm({
  spaceId,
  onSuccess,
  onCancel,
}: Readonly<CreateFolderFormProps>) {
  const { workspaceId } = useWorkspace();
  const [createFolderMutation, { isLoading: isCreating }] = useCreateFolderMutation();
  const [state, dispatch] = useReducer(folderFormReducer, initialFolderState);

  // Lazy-load Space Record details and items (including statuses)
  const { data: space } = useGetSpaceDetailQuery(spaceId);
  useGetSpaceItemsQuery(spaceId);
  const spaceStatuses = useSpaceStatuses(spaceId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!state.name.trim()) return;

    try {
      const { id: newId } = await createFolderMutation({
        workspaceId,
        body: {
          spaceId,
          name: state.name,
          color: state.color,
          icon: state.icon,
          statusId: state.selectedStatusId,
          priority: state.priority,
          startDate: state.startDate?.toISOString(),
          dueDate: state.dueDate?.toISOString(),
        },
      }).unwrap();
      toast.success("Folder created");
      dispatch({ type: "RESET" });
      onSuccess?.(newId);
    } catch {
      toast.error("Failed to create folder",);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-2.5 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker
            icon={state.icon}
            color={state.color}
            onChange={(i, c) => {
              dispatch({ type: "SET_ICON", payload: i });
              dispatch({ type: "SET_COLOR", payload: c });
            }}
          />
          <input
            placeholder="Folder name"
            aria-label="Folder name"
            value={state.name}
            onChange={(e) => dispatch({ type: "SET_NAME", payload: e.target.value })}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
            onKeyDown={(e) => {
              if (e.key === " ") {
                e.stopPropagation();
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
          workflowId={space?.workflowId}
          statuses={spaceStatuses}
          align="start"
          trigger={
            <AttributeButton icon={state.selectedStatusId ? undefined : Icons.Circle}>
              {state.selectedStatusId ? (
                <StatusBadge
                  status={spaceStatuses.find((s: Status) => s.id?.toLowerCase() === state.selectedStatusId?.toLowerCase())}
                  showIcon={true}
                />
              ) : (
                "Status"
              )}
            </AttributeButton>
          }
        />

        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton>
              <PriorityBadge priority={state.priority} />
            </AttributeButton>
          </PopoverTrigger>
          <PopoverContent
            className="w-32 p-1 bg-popover border border-border shadow-md rounded-md"
            align="start"
          >
            <div className="flex flex-col gap-0.5">
              {[
                Priority.Low,
                Priority.Normal,
                Priority.High,
                Priority.Urgent,
              ].map((p) => (
                <button
                  key={p}
                  type="button"
                  className="px-1 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center cursor-pointer"
                  onClick={() => dispatch({ type: "SET_PRIORITY", payload: p })}
                >
                  <PriorityBadge
                    priority={p}
                    className="w-full justify-start"
                  />
                </button>
              ))}
            </div>
          </PopoverContent>
        </Popover>

        <DateSelect
          startDate={state.startDate?.toISOString()}
          dueDate={state.dueDate?.toISOString()}
          onStartDateChange={(date) => dispatch({ type: "SET_START_DATE", payload: date })}
          onDueDateChange={(date) => dispatch({ type: "SET_DUE_DATE", payload: date })}
          align="start"
          size="sm"
          triggerClassName="h-6 px-2 text-[10px] font-medium rounded-md border border-transparent bg-muted/30 hover:bg-muted/50 text-muted-foreground transition-all cursor-pointer"
        />
      </div>

      {/* Footer */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-end gap-2 border-t border-border/10">
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
          {isCreating ? "Creating..." : "Create Folder"}
        </Button>
      </div>
    </form>
  );
}

import { useState, useEffect, useMemo } from "react";
import { useCreateTask } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Priority } from "@/types/priority";
import { Circle, Command, User } from "lucide-react";
import { toast } from "sonner";
import {
  AttributeButton,
  IconColorPicker,
  SimpleDatePicker,
} from "./form-elements";
import { useAvailableStatuses } from "../../api";
import { useRegistryStore } from "../../context/use-registry-store";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";

interface CreateTaskFormProps {
  parentId: string;
  parentType: EntityLayerType;
  onSuccess?: (task: any) => void;
  onCancel?: () => void;
}

export function CreateTaskForm({
  parentId,
  parentType,
  onSuccess,
  onCancel,
}: CreateTaskFormProps) {
  const { workspaceId } = useWorkspace();
  const createTask = useCreateTask(workspaceId);
  const [name, setName] = useState("");
  const [priority, setPriority] = useState<Priority>(Priority.Normal);
  const [icon, setIcon] = useState("Circle");
  const [color, setColor] = useState("#94a3b8");
  const [selectedStatusId, setSelectedStatusId] = useState<
    string | undefined
  >();
  const [startDate, setStartDate] = useState<Date | undefined>();
  const [dueDate, setDueDate] = useState<Date | undefined>();

  const { setStatuses, setSpaceStatuses, setFolderStatuses } =
    useRegistryStore();
  const isSpace = parentType === "ProjectSpace";

  const { data: fetchedStatuses } = useAvailableStatuses(
    isSpace ? parentId : undefined,
    isSpace ? undefined : parentId,
  );

  useEffect(() => {
    if (fetchedStatuses) {
      const store = useRegistryStore.getState();
      const currentIds = isSpace
        ? store.spaceStatuses[parentId]
        : store.folderStatuses[parentId];
      const newIds = fetchedStatuses.map((s) => s.id);

      if (JSON.stringify(currentIds || []) !== JSON.stringify(newIds)) {
        setStatuses(fetchedStatuses);
        if (isSpace) {
          setSpaceStatuses(parentId, newIds);
        } else {
          setFolderStatuses(parentId, newIds);
        }
      }
    }
  }, [
    fetchedStatuses,
    parentId,
    isSpace,
    setStatuses,
    setSpaceStatuses,
    setFolderStatuses,
  ]);

  const statusIds = useRegistryStore((state) => {
    return isSpace ? state.spaceStatuses[parentId] : state.folderStatuses[parentId];
  });
  
  const statuses = useMemo(() => {
    const allStatuses = useRegistryStore.getState().statuses;
    return (statusIds || []).map(id => allStatuses[id]).filter(Boolean);
  }, [statusIds, isSpace, parentId]);

  const onSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createTask.mutateAsync({
        parentId,
        parentType,
        name,
        priority: priority as any,
        icon,
        color,
        statusId: selectedStatusId,
        startDate: startDate?.toISOString(),
        dueDate: dueDate?.toISOString(),
      });
      toast.success("Task created");
      onSuccess?.((result as any).data);
    } catch (error) {
      toast.error("Failed to create task");
    }
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-2 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker
            icon={icon}
            color={color}
            onChange={(i, c) => {
              setIcon(i);
              setColor(c);
            }}
          />
          <textarea
            placeholder="Task title"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none resize-none min-h-[22px]"
            autoFocus
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
      <div className="px-3 py-1.5 flex flex-wrap items-center gap-1.5 border-t border-border/5">
        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton icon={selectedStatusId ? undefined : Circle}>
              {selectedStatusId ? (
                <StatusBadge
                  status={statuses.find((s) => s.id === selectedStatusId)}
                />
              ) : (
                "Status"
              )}
            </AttributeButton>
          </PopoverTrigger>
          <PopoverContent
            className="w-48 p-1 bg-popover border border-border shadow-md rounded-md"
            align="start"
          >
            <div className="flex flex-col gap-0.5">
              {statuses.map((status) => (
                <button
                  key={status.id}
                  type="button"
                  className="px-1 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center"
                  onClick={() => setSelectedStatusId(status.id)}
                >
                  <StatusBadge
                    status={status}
                    className="w-full justify-start"
                  />
                </button>
              ))}
              {statuses.length === 0 && (
                <span className="text-xs text-muted-foreground p-2">
                  No statuses found
                </span>
              )}
            </div>
          </PopoverContent>
        </Popover>

        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton>
              <PriorityBadge priority={priority} />
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
                  className="px-1 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center"
                  onClick={() => setPriority(p)}
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

        <SimpleDatePicker
          value={startDate}
          onChange={setStartDate}
          label="Start Date"
        />
        <SimpleDatePicker
          value={dueDate}
          onChange={setDueDate}
          label="Due Date"
        />

        <AttributeButton icon={User} className="ml-auto">
          Assignee
        </AttributeButton>
      </div>

      {/* Footer Actions */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-between border-t border-border/10">
        <div className="flex items-center gap-1">
          <div className="px-1.5 py-0.5 rounded border border-border/50 bg-muted/30 text-[9px] font-medium text-muted-foreground flex items-center gap-1">
            <Command className="h-2 w-2" /> Enter
          </div>
          <span className="text-[10px] text-muted-foreground/30 font-medium">
            to create
          </span>
        </div>

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
            disabled={!name.trim() || createTask.isPending}
            className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
          >
            {createTask.isPending ? "Creating..." : "Create Task"}
          </Button>
        </div>
      </div>
    </form>
  );
}

import { useState } from "react";
import { useCreateFolderMutation } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import {
  IconColorPicker,
  AttributeButton,
  SimpleDatePicker,
} from "./form-elements";
import * as Icons from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { StatusSelect } from "@/components/status-select";
import { useGetSpaceDetailQuery, useGetSpaceItemsQuery, useSpaceStatuses } from "../../contents/views/space/space-api";
import type { Status } from "@/types/status";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface CreateFolderFormProps {
  spaceId: string;
  onSuccess?: (id: string) => void;
  onCancel?: () => void;
}

export function CreateFolderForm({
  spaceId,
  onSuccess,
  onCancel,
}: CreateFolderFormProps) {
  const { workspaceId, registry } = useWorkspace();
  const [createFolderMutation, { isLoading: isCreating }] = useCreateFolderMutation();
  const [name, setName] = useState("");
  const [icon, setIcon] = useState("Folder");
  const [color, setColor] = useState("#6366f1");
  const [selectedStatusId, setSelectedStatusId] = useState<string | null>(null);
  const [priority, setPriority] = useState<Priority>(Priority.Normal);
  const [startDate, setStartDate] = useState<Date | undefined>();
  const [dueDate, setDueDate] = useState<Date | undefined>();

  // Lazy-load Space Record details and items (including statuses)
  const { data: space } = useGetSpaceDetailQuery(spaceId);
  useGetSpaceItemsQuery(spaceId);
  const spaceStatuses = useSpaceStatuses(spaceId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createFolderMutation({
        workspaceId,
        body: {
          spaceId,
          name,
          color,
          icon,
          statusId: selectedStatusId,
          priority,
          startDate: startDate?.toISOString(),
          dueDate: dueDate?.toISOString(),
        },
      }).unwrap();
      toast.success("Folder created");

      // Reset form state cleanly upon success
      setName("");
      setIcon("Folder");
      setColor("#6366f1");
      setSelectedStatusId(null);
      setPriority(Priority.Normal);
      setStartDate(undefined);
      setDueDate(undefined);

      onSuccess?.((result as any).id);
    } catch (error) {
      toast.error("Failed to create folder");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-2.5 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker
            icon={icon}
            color={color}
            onChange={(i, c) => {
              setIcon(i);
              setColor(c);
            }}
          />
          <input
            placeholder="Folder name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
            autoFocus
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
          value={selectedStatusId || undefined}
          onChange={(statusId) => setSelectedStatusId(statusId)}
          workflowId={space?.workflowId}
          align="start"
          trigger={
            <AttributeButton icon={selectedStatusId ? undefined : Icons.Circle}>
              {selectedStatusId ? (
                <StatusBadge
                  status={spaceStatuses.find((s: Status) => s.id === selectedStatusId)}
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
                  className="px-1 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center cursor-pointer"
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
          disabled={!name.trim() || isCreating}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {isCreating ? "Creating..." : "Create Folder"}
        </Button>
      </div>
    </form>
  );
}

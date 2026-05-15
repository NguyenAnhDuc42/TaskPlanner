import { useState, useEffect, useMemo } from "react";
import { useCreateFolder } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { useWorkspaceDataStore } from "../../context/use-workspace-data-store";
import { toast } from "sonner";
import {
  IconColorPicker,
  PrivacyToggle,
  AttributeButton,
  SimpleDatePicker,
} from "./form-elements";
import * as Icons from "lucide-react";
import { useAvailableStatuses } from "../../api";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { StatusBadge } from "@/components/status-badge";

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
  const { workspaceId } = useWorkspace();
  const createFolder = useCreateFolder(workspaceId);
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [icon, setIcon] = useState("Folder");
  const [color, setColor] = useState("#6366f1");
  const [selectedStatusId, setSelectedStatusId] = useState<string | null>(null);
  const [startDate, setStartDate] = useState<Date | undefined>();
  const [dueDate, setDueDate] = useState<Date | undefined>();

  const { setStatuses, setSpaceStatuses } = useWorkspaceDataStore();

  const { data: fetchedStatuses } = useAvailableStatuses(spaceId);

  useEffect(() => {
    if (fetchedStatuses) {
      const currentIds =
        useWorkspaceDataStore.getState().spaceStatuses[spaceId] || [];
      const newIds = fetchedStatuses.map((s) => s.statusId);
      if (JSON.stringify(currentIds) !== JSON.stringify(newIds)) {
        setStatuses(fetchedStatuses);
        setSpaceStatuses(spaceId, newIds);
      }
    }
  }, [fetchedStatuses, spaceId, setStatuses, setSpaceStatuses]);

  const statusIds = useWorkspaceDataStore((state) => state.spaceStatuses[spaceId]);

  const allStatuses = useWorkspaceDataStore((state) => state.statuses);

  const statuses = useMemo(() => {
    return (statusIds || []).map((id) => allStatuses[id]).filter(Boolean);
  }, [statusIds, allStatuses]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createFolder.mutateAsync({
        spaceId,
        name,
        isPrivate,
        color,
        icon,
        statusId: selectedStatusId,
        startDate: startDate?.toISOString(),
        dueDate: dueDate?.toISOString(),
      });
      toast.success("Folder created");
      onSuccess?.((result as any).data);
    } catch (error) {
      toast.error("Failed to create folder");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-4 pb-2">
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
      <div className="px-3 py-1.5 flex flex-wrap items-center gap-1.5 border-t border-border/5">
        <PrivacyToggle isPrivate={isPrivate} onChange={setIsPrivate} />

        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton icon={selectedStatusId ? undefined : Icons.Circle}>
              {selectedStatusId ? (
                <StatusBadge
                  status={statuses.find((s) => s.statusId === selectedStatusId)}
                  showIcon={true}
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
                  key={status.statusId}
                  type="button"
                  className="px-1 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center"
                  onClick={() => setSelectedStatusId(status.statusId)}
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
          disabled={!name.trim() || createFolder.isPending}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {createFolder.isPending ? "Creating..." : "Create Folder"}
        </Button>
      </div>
    </form>
  );
}

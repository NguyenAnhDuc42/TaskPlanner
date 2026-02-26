import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Plus,
  GripVertical,
  MoreHorizontal,
  Check,
  ChevronLeft,
  Trash2,
} from "lucide-react";
import { useStatuses, useSyncStatuses } from "../statuses-api";
import { StatusCategory, type StatusDto } from "../status-types";
import { cn } from "@/lib/utils";
import { ColorPicker } from "@/components/color-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  layerId: string;
  layerType: string;
  layerName: string;
}

interface LocalStatus extends Partial<StatusDto> {
  id?: string;
  tempId?: string;
  name: string;
  color: string;
  category: StatusCategory;
  isDeleted?: boolean;
}

interface StatusItemProps {
  status: LocalStatus;
  onUpdate: (updates: Partial<LocalStatus>) => void;
  onDelete: () => void;
}

function StatusItem({ status, onUpdate, onDelete }: StatusItemProps) {
  if (status.isDeleted) return null;

  return (
    <div
      className="group flex items-center gap-2 bg-muted/20 border transition-all p-1 rounded-md"
      style={{ borderColor: status.color + "40" }}
    >
      <GripVertical className="h-3.5 w-3.5 text-muted-foreground/10 group-hover:text-muted-foreground/20 cursor-grab shrink-0" />

      <Popover>
        <PopoverTrigger asChild>
          <div
            className="h-4 w-4 rounded-full border-2 flex items-center justify-center cursor-pointer transition-transform hover:scale-110 shrink-0 shadow-sm"
            style={{
              borderColor: status.color,
              backgroundColor:
                status.category === StatusCategory.Done
                  ? status.color
                  : "transparent",
            }}
          >
            {status.category === StatusCategory.Done && (
              <Check className="h-2 w-2 text-white stroke-[3px]" />
            )}
          </div>
        </PopoverTrigger>
        <PopoverContent
          className="w-auto p-0 border-none shadow-2xl rounded-xl"
          align="start"
        >
          <ColorPicker
            value={status.color}
            onChange={(color) => onUpdate({ color })}
          />
        </PopoverContent>
      </Popover>

      <Input
        value={status.name}
        spellCheck={false}
        onChange={(e) => onUpdate({ name: e.target.value.toUpperCase() })}
        className="h-8 border-none bg-transparent focus-visible:ring-0 font-bold uppercase text-[10px] tracking-wide flex-1"
      />

      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-muted-foreground/40 hover:text-red-500 hover:bg-red-50 transition-colors"
          onClick={onDelete}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-muted-foreground/40"
        >
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

interface StatusCategorySectionProps {
  cat: { id: StatusCategory; label: string; color: string };
  catStatuses: LocalStatus[];
  onAdd: () => void;
  onUpdate: (idOrTempId: string, updates: Partial<LocalStatus>) => void;
  onDelete: (idOrTempId: string) => void;
}

function StatusCategorySection({
  cat,
  catStatuses,
  onAdd,
  onUpdate,
  onDelete,
}: StatusCategorySectionProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span
            className={cn(
              "text-xs font-bold uppercase tracking-wider",
              cat.color,
            )}
          >
            {cat.label}
          </span>
        </div>
        <button
          className="h-7 w-7 flex items-center justify-center hover:bg-muted rounded-full transition-colors"
          onClick={onAdd}
        >
          <Plus className="h-4 w-4" />
        </button>
      </div>

      <div className="space-y-2">
        {catStatuses.map((status) => (
          <StatusItem
            key={status.id || status.tempId}
            status={status}
            onUpdate={(updates) =>
              onUpdate(status.id || status.tempId!, updates)
            }
            onDelete={() => onDelete(status.id || status.tempId!)}
          />
        ))}

        <Button
          variant="ghost"
          className="w-full h-10 border-dashed border-2 text-muted-foreground/60 hover:text-foreground hover:border-accent/40 bg-transparent py-2 justify-center gap-2 font-medium text-xs transition-all rounded-md"
          onClick={onAdd}
        >
          <Plus className="h-3.5 w-3.5" />
          ADD STATUS
        </Button>
      </div>
    </div>
  );
}

export function StatusManagementDialog({
  open,
  onOpenChange,
  layerId,
  layerType,
  layerName,
}: Props) {
  const { data: statuses } = useStatuses(layerId, layerType);
  const syncStatuses = useSyncStatuses();

  const [localStatuses, setLocalStatuses] = useState<LocalStatus[]>([]);

  useEffect(() => {
    if (statuses && open) {
      setLocalStatuses(statuses.map((s) => ({ ...s, isDeleted: false })));
    }
  }, [statuses, open]);

  const categories = [
    {
      id: StatusCategory.NotStarted,
      label: "Not started",
      color: "text-gray-400",
    },
    { id: StatusCategory.Active, label: "Active", color: "text-blue-500" },
    { id: StatusCategory.Done, label: "Done", color: "text-green-500" },
    { id: StatusCategory.Closed, label: "Closed", color: "text-[#10b981]" },
  ];

  const handleAddLocal = (category: StatusCategory) => {
    const newStatus: LocalStatus = {
      tempId: Math.random().toString(36).substr(2, 9),
      name: "NEW STATUS",
      color: "#808080",
      category,
      isDeleted: false,
    };
    setLocalStatuses([...localStatuses, newStatus]);
  };

  const handleUpdateLocal = (
    idOrTempId: string,
    updates: Partial<LocalStatus>,
  ) => {
    setLocalStatuses((prev) =>
      prev.map((s) =>
        s.id === idOrTempId || s.tempId === idOrTempId
          ? { ...s, ...updates }
          : s,
      ),
    );
  };

  const handleDeleteLocal = (idOrTempId: string) => {
    setLocalStatuses((prev) =>
      prev.map((s) =>
        s.id === idOrTempId || s.tempId === idOrTempId
          ? { ...s, isDeleted: true }
          : s,
      ),
    );
  };

  const handleApply = async () => {
    const payload = {
      layerId,
      layerType,
      statuses: localStatuses.map((s) => ({
        id: s.id,
        name: s.name,
        color: s.color,
        category: s.category,
        isDeleted: !!s.isDeleted,
      })),
    };

    await syncStatuses.mutateAsync(payload);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl h-[70vh] flex flex-col p-0 gap-0 overflow-hidden shadow-xl rounded-xl border-none">
        <DialogHeader className="p-4 border-b shrink-0 flex-row items-center justify-between bg-muted/5 backdrop-blur-md z-20">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 rounded-full hover:bg-muted"
              onClick={() => onOpenChange(false)}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <DialogTitle className="text-lg font-bold">
              Manage Statuses for{" "}
              <span className="text-blue-500">{layerName}</span>
            </DialogTitle>
          </div>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto min-h-0 bg-background">
          <div className="max-w-lg mx-auto p-6 space-y-8 pb-20">
            {categories.map((cat) => {
              const catStatuses = localStatuses.filter(
                (s) => s.category === cat.id && !s.isDeleted,
              );

              return (
                <StatusCategorySection
                  key={cat.id}
                  cat={cat}
                  catStatuses={catStatuses}
                  onAdd={() => handleAddLocal(cat.id)}
                  onUpdate={handleUpdateLocal}
                  onDelete={handleDeleteLocal}
                />
              );
            })}
          </div>
        </div>

        <div className="p-4 border-t flex justify-end gap-3 bg-muted/5 shrink-0 z-20">
          <Button
            variant="ghost"
            size="sm"
            className="font-bold px-6 h-10 rounded-md"
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
          <Button
            variant="secondary"
            size="sm"
            className="bg-[#3a3b3c] text-white hover:bg-[#4a4b4c] font-bold px-8 py-2 h-10 shadow-sm transition-all rounded-md"
            onClick={handleApply}
            disabled={syncStatuses.isPending}
          >
            {syncStatuses.isPending ? "Applying..." : "Apply changes"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

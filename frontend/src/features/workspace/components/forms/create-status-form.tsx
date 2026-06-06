import { useEffect, useState, useRef, useCallback, useMemo } from "react";
import { Plus, GripVertical, Trash2, HelpCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { StatusCategory } from "@/types/status-category";
import type { Status } from "@/types/status";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useUpdateWorkflowStatuses } from "@/features/workspace/api";
import { RowAction } from "@/types/row-action";
import type { StatusUpdatePayload } from "@/features/workspace/api";
import { fractionalBetween } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";


interface CreateStatusFormProps {
  isOpen: boolean;
  onClose: () => void;
  workflowId?: string;
  currentStatuses?: Status[];
  onApplyChanges?: (statuses: Status[]) => void;
}

const PRESET_COLORS = [
  "#ef4444",
  "#f97316",
  "#f59e0b",
  "#10b981",
  "#06b6d4",
  "#3b82f6",
  "#6366f1",
  "#8b5cf6",
  "#ec4899",
  "#64748b",
];

function StatusColorPicker({
  selectedColor,
  onSelectColor,
}: {
  selectedColor: string;
  onSelectColor: (color: string) => void;
}) {
  const [customColor, setCustomColor] = useState(selectedColor);
  const colorInputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="flex items-center gap-1.5 p-2 bg-background border border-border/40 rounded-lg shadow-xl w-[212px] flex-wrap">
      {PRESET_COLORS.map((color) => (
        <button
          key={color}
          className={cn(
            "w-6 h-6 rounded-full flex-shrink-0 transition-all duration-150 hover:scale-110 active:scale-95",
            selectedColor === color
              ? "ring-2 ring-primary ring-offset-1 ring-offset-background"
              : "ring-1 ring-border/50 hover:ring-2 hover:ring-primary/50"
          )}
          style={{ backgroundColor: color }}
          onClick={() => onSelectColor(color)}
          type="button"
        />
      ))}
      <button
        onClick={() => {
          setTimeout(() => colorInputRef.current?.click(), 0);
        }}
        className="w-6 h-6 rounded-full flex-shrink-0 flex items-center justify-center bg-gradient-to-br from-red-500 via-green-500 to-blue-500 hover:shadow-lg transition-all hover:scale-110 ml-0.5"
        title="Pick custom color"
        type="button"
      >
        <input
          ref={colorInputRef}
          type="color"
          value={customColor}
          onChange={(e) => {
            setCustomColor(e.target.value);
            onSelectColor(e.target.value);
          }}
          className="opacity-0 w-0 h-0 cursor-pointer"
        />
      </button>
    </div>
  );
}

function SortableStatusItem({
  status,
  onUpdateName,
  onUpdateColor,
  onDelete,
}: {
  status: Status;
  onUpdateName: (id: string, name: string) => void;
  onUpdateColor: (id: string, color: string) => void;
  onDelete: (id: string) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: status.id });

  const [isOpen, setIsOpen] = useState(false);

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <div
        ref={setNodeRef}
        style={style}
        onClick={() => setIsOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            setIsOpen(true);
          }
        }}
        tabIndex={0}
        role="button"
        className={cn(
          "flex items-center gap-2 bg-muted/20 hover:bg-muted/30 px-2.5 h-8 rounded-md border border-border/40 group cursor-pointer outline-none focus-visible:ring-1 focus-visible:ring-ring",
          isDragging && "opacity-50 shadow-lg z-50"
        )}
      >
        <button
          className="h-3 w-3 text-muted-foreground/30 cursor-grab active:cursor-grabbing group-hover:text-muted-foreground/60 shrink-0 touch-none"
          {...attributes}
          {...listeners}
          onClick={(e) => e.stopPropagation()}
        >
          <GripVertical className="h-3 w-3" />
        </button>

        <PopoverTrigger asChild>
          <div className="flex items-center gap-2 flex-1 h-full">
            <div
              className="h-2.5 w-2.5 rounded-full shrink-0 transition-transform group-hover:scale-110"
              style={{ backgroundColor: status.color }}
            />

            <input
              className="text-xs font-medium flex-1 bg-transparent border-none outline-none focus:ring-0 p-0 text-foreground cursor-text"
              value={status.name}
              onFocus={() => setIsOpen(true)}
              onPointerDown={(e) => e.stopPropagation()}
              onClick={(e) => {
                e.stopPropagation();
                setIsOpen(true);
              }}
              onChange={(e) => onUpdateName(status.id, e.target.value)}
            />
          </div>
        </PopoverTrigger>

        <PopoverContent className="p-0 border-none bg-transparent shadow-none w-auto" side="right" sideOffset={8}>
          <StatusColorPicker
            selectedColor={status.color}
            onSelectColor={(c) => onUpdateColor(status.id, c)}
          />
        </PopoverContent>

        <button
          className="text-muted-foreground/30 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all shrink-0"
          onClick={(e) => {
            e.stopPropagation();
            onDelete(status.id);
          }}
          type="button"
        >
          <Trash2 className="h-3 w-3" />
        </button>
      </div>
    </Popover>
  );
}

// ── Main Form ────────────────────────────────────────────────────────
export function CreateStatusForm({
  isOpen,
  onClose,
  workflowId,
  currentStatuses,
  onApplyChanges,
}: CreateStatusFormProps) {
  const { registry, workspaceId: currentWorkspaceId } = useWorkspace();
  const { mutate: updateStatuses } = useUpdateWorkflowStatuses();

  const allStatuses = useSelector(statusSelectors.selectAll);

  const resolvedCurrentStatuses = useMemo(() => {
    if (currentStatuses) {
      return currentStatuses;
    }
    if (!workflowId) return [];
    return allStatuses
      .filter((s: Status) => s.workflowId?.toLowerCase() === workflowId.toLowerCase())
      .sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));
  }, [currentStatuses, allStatuses, workflowId]);

  const [localStatuses, setLocalStatuses] = useState<Status[]>(resolvedCurrentStatuses);
  const [name, setName] = useState("");
  const [addingToCategory, setAddingToCategory] = useState<StatusCategory | null>(null);

  useEffect(() => {
    setLocalStatuses(resolvedCurrentStatuses);
  }, [resolvedCurrentStatuses]);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const groupedStatuses = {
    [StatusCategory.NotStarted]: localStatuses.filter(
      (s) => s.category === StatusCategory.NotStarted
    ),
    [StatusCategory.Active]: localStatuses.filter(
      (s) => s.category === StatusCategory.Active
    ),
    [StatusCategory.Done]: localStatuses.filter(
      (s) => s.category === StatusCategory.Done
    ),
    [StatusCategory.Closed]: localStatuses.filter(
      (s) => s.category === StatusCategory.Closed
    ),
  };

  const handleUpdateName = useCallback((id: string, newName: string) => {
    setLocalStatuses((prev) =>
      prev.map((s) => (s.id === id ? { ...s, name: newName } : s))
    );
  }, []);

  const handleUpdateColor = useCallback((id: string, color: string) => {
    setLocalStatuses((prev) =>
      prev.map((s) => (s.id === id ? { ...s, color } : s))
    );
  }, []);

  const handleDelete = useCallback((id: string) => {
    setLocalStatuses((prev) => prev.filter((s) => s.id !== id));
  }, []);

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      setLocalStatuses((prev) => {
        const oldIndex = prev.findIndex((s) => s.id === active.id);
        const newIndex = prev.findIndex((s) => s.id === over.id);

        if (oldIndex === -1 || newIndex === -1) return prev;

        return arrayMove(prev, oldIndex, newIndex);
      });
    },
    []
  );

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-sm bg-background border-border/40 text-foreground p-0 rounded-md">
        <DialogHeader className="border-b border-border/30 px-4 py-2.5">
          <DialogTitle className="text-base font-bold tracking-tight">
            Manage Statuses
          </DialogTitle>
        </DialogHeader>

        <div className="p-4 space-y-4 overflow-y-auto max-h-[60vh] [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent">
          {Object.entries(groupedStatuses).map(([cat, statuses]) => (
            <div key={cat} className="space-y-2">
              {/* Category Header */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground/50">
                    {cat}
                  </span>
                  <HelpCircle className="h-3 w-3 text-muted-foreground/30" />
                </div>
                <button
                  type="button"
                  className="text-muted-foreground/40 hover:text-foreground"
                  onClick={(e) => {
                    e.preventDefault();
                    setAddingToCategory(cat as StatusCategory);
                    setName("");
                  }}
                  onPointerDown={(e) => e.preventDefault()}
                >
                  <Plus className="h-3.5 w-3.5" />
                </button>
              </div>

              {/* Status List with DnD */}
              <DndContext
                sensors={sensors}
                collisionDetection={closestCenter}
                modifiers={[restrictToVerticalAxis]}
                onDragEnd={handleDragEnd}
              >
                <SortableContext
                  items={statuses.map((s) => s.id)}
                  strategy={verticalListSortingStrategy}
                >
                  <div className="space-y-1.5">
                    {statuses.map((s) => (
                      <SortableStatusItem
                        key={s.id}
                        status={s}
                        onUpdateName={handleUpdateName}
                        onUpdateColor={handleUpdateColor}
                        onDelete={handleDelete}
                      />
                    ))}
                  </div>
                </SortableContext>
              </DndContext>

              {/* Add Status Row */}
              {addingToCategory === cat ? (
                <div className="flex items-center gap-2 bg-muted/20 px-2.5 h-8 rounded-md border border-border/40">
                  <div className="h-2 w-2 rounded-full shrink-0 bg-muted-foreground/30" />
                  <input
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="Status name"
                    aria-label="Status name"
                    className="flex-1 h-6 bg-transparent p-0 text-xs focus:outline-none placeholder:text-muted-foreground/30"
                    onBlur={() => {
                      if (name.trim()) {
                        const newStatus: Status = {
                          id: `temp-${Date.now()}`,
                          workflowId: workflowId || "",
                          name: name.trim(),
                          color: PRESET_COLORS[0],
                          category: cat as StatusCategory,
                          orderKey: "",
                        };
                        setLocalStatuses([...localStatuses, newStatus]);
                      }
                      setAddingToCategory(null);
                      setName("");
                    }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && name.trim()) {
                        const newStatus: Status = {
                          id: `temp-${Date.now()}`,
                          workflowId: workflowId || "",
                          name: name.trim(),
                          color: PRESET_COLORS[0],
                          category: cat as StatusCategory,
                          orderKey: "",
                        };
                        setLocalStatuses([...localStatuses, newStatus]);
                        setAddingToCategory(null);
                        setName("");
                      } else if (e.key === "Escape") {
                        setAddingToCategory(null);
                        setName("");
                      }
                    }}
                  />
                </div>
              ) : (
                <button
                  type="button"
                  className="w-full flex items-center gap-2 bg-transparent hover:bg-muted/10 px-2.5 h-8 rounded-md border border-dashed border-border/20 text-muted-foreground/40 hover:text-muted-foreground hover:border-border/40 transition-colors"
                  onClick={(e) => {
                    e.preventDefault();
                    setAddingToCategory(cat as StatusCategory);
                    setName("");
                  }}
                  onPointerDown={(e) => e.preventDefault()}
                >
                  <Plus className="h-3 w-3 ml-1" />
                  <span className="text-xs font-medium">Add status</span>
                </button>
              )}
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="border-t border-border/30 px-4 py-2.5 flex justify-end bg-muted/5">
          <Button
            className="h-8 text-xs gap-1.5 rounded-md"
            onClick={() => {
              if (workflowId) {
                const { payloads, clonedStatuses } = buildStatusUpdatePayloads(localStatuses, resolvedCurrentStatuses || []);
                updateStatuses({ 
                  workflowId, 
                  workspaceId: registry.workspaceId || currentWorkspaceId, 
                  statuses: payloads, 
                  optimisticStatuses: clonedStatuses 
                });
              } else {
                onApplyChanges?.(localStatuses);
              }
              onClose();
            }}
          >
            Apply changes
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function buildStatusUpdatePayloads(
  localStatuses: any[],
  resolvedCurrentStatuses: any[]
): { payloads: StatusUpdatePayload[]; clonedStatuses: any[] } {
  const originalStatuses = resolvedCurrentStatuses || [];
  const newIds = new Set(localStatuses.map(s => s.id));
  const payloads: StatusUpdatePayload[] = [];

  // Deletes
  for (const s of originalStatuses) {
    const sid = s.id;
    if (!newIds.has(sid)) {
      payloads.push({
        id: sid,
        name: s.name,
        color: s.color,
        category: s.category,
        previousOrderKey: null,
        nextOrderKey: null,
        action: RowAction.Delete,
      });
    }
  }

  // Clone local statuses to prevent read-only property mutation
  const clonedStatuses = localStatuses.map(s => ({ ...s }));

  // Group cloned statuses by category
  const categoryGroups: Record<string, any[]> = {};
  for (const s of clonedStatuses) {
    if (!categoryGroups[s.category]) {
      categoryGroups[s.category] = [];
    }
    categoryGroups[s.category].push(s);
  }

  // Creates & Updates
  for (const s of clonedStatuses) {
    const isNew = s.id && typeof s.id === 'string' && s.id.startsWith("temp-");
    const catGroup = categoryGroups[s.category] || [];
    const idx = catGroup.findIndex(item => item.id === s.id);
    
    const prevKey = idx > 0 ? catGroup[idx - 1].orderKey || null : null;
    const nextKey = idx < catGroup.length - 1 ? catGroup[idx + 1].orderKey || null : null;
    
    s.orderKey = fractionalBetween(prevKey, nextKey);

    payloads.push({
      id: isNew ? null : s.id,
      name: s.name,
      color: s.color,
      category: s.category,
      previousOrderKey: prevKey,
      nextOrderKey: nextKey,
      action: isNew ? RowAction.Create : RowAction.Update,
    });
  }

  return { payloads, clonedStatuses };
}

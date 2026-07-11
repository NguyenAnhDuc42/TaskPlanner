import { useState, useRef, useCallback, useMemo, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { Plus, Trash2, Check, GripVertical, X } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  DragOverlay,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { Status } from "@/types/status";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { StatusMutations, type StatusUpdateValue } from "@/mutations/status.mutations";
import { RowAction } from "@/types/row-action";
import { fractionalBetween } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { createPortal } from "react-dom";

// ─── Types ────────────────────────────────────────────────────────────────────

interface CreateStatusFormProps {
  isOpen: boolean;
  onClose: () => void;
  spaceId?: string;
  currentStatuses?: Status[];
  onApplyChanges?: (statuses: Status[]) => void;
}

// ─── Constants ────────────────────────────────────────────────────────────────

const PRESET_COLORS = [
  "#ef4444", "#f97316", "#f59e0b", "#eab308",
  "#22c55e", "#10b981", "#06b6d4", "#3b82f6",
  "#6366f1", "#8b5cf6", "#ec4899", "#64748b",
];

// ─── Color Picker ─────────────────────────────────────────────────────────────

function ColorPicker({
  selectedColor,
  onSelect,
}: {
  selectedColor: string;
  onSelect: (c: string) => void;
}) {
  const [hexInput, setHexInput] = useState(selectedColor);

  const handleHex = (val: string) => {
    const normalized = val.startsWith("#") ? val : `#${val}`;
    setHexInput(normalized);
    if (/^#[0-9a-fA-F]{6}$/.test(normalized)) onSelect(normalized);
  };

  return (
    <div className="p-2 bg-popover border border-border/40 rounded-lg shadow-2xl w-44 flex flex-wrap gap-1.5">
      {PRESET_COLORS.map((c) => (
        <button
          key={c}
          type="button"
          className={cn(
            "h-5 w-5 rounded-full shrink-0 transition-all duration-100 hover:scale-110 active:scale-95",
            selectedColor === c
              ? "ring-2 ring-white/80 ring-offset-1 ring-offset-background scale-110"
              : "ring-1 ring-white/10"
          )}
          style={{ backgroundColor: c }}
          onClick={() => { onSelect(c); setHexInput(c); }}
        />
      ))}
      <input
        type="text"
        value={hexInput}
        maxLength={7}
        placeholder="#000000"
        onChange={(e) => handleHex(e.target.value)}
        onMouseDown={(e) => e.stopPropagation()}
        className="w-full mt-1 h-6 text-[10px] px-2 rounded border border-border/30 bg-muted/20 font-mono outline-none focus:border-primary/50 text-foreground/70"
      />
    </div>
  );
}

// ─── Sortable Status Card ─────────────────────────────────────────────────────

function StatusCard({
  status,
  onUpdateName,
  onUpdateColor,
  onDelete,
  overlay = false,
}: {
  status: Status;
  onUpdateName: (id: string, name: string) => void;
  onUpdateColor: (id: string, color: string) => void;
  onDelete: (id: string) => void;
  overlay?: boolean;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: status.id });

  const [pickerOpen, setPickerOpen] = useState(false);
  const pickerRef = useRef<HTMLDivElement>(null);

  // Close color picker on outside click
  useEffect(() => {
    if (!pickerOpen) return;
    const handler = (e: MouseEvent) => {
      if (pickerRef.current && !pickerRef.current.contains(e.target as Node)) {
        setPickerOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [pickerOpen]);

  const style = overlay
    ? {}
    : { transform: CSS.Transform.toString(transform), transition };

  return (
    <div
      ref={overlay ? undefined : setNodeRef}
      style={style}
      className={cn(
        "group relative flex items-center gap-2 h-8 px-2 rounded-md border bg-card/60 hover:bg-card/90 border-border/30 hover:border-border/60 transition-colors duration-150 select-none",
        isDragging && !overlay && "opacity-0",
        overlay && "shadow-2xl rotate-1 scale-105 opacity-90 bg-card border-border/60"
      )}
    >
      {/* Drag handle */}
      <button
        type="button"
        className="text-muted-foreground/20 hover:text-muted-foreground/60 cursor-grab active:cursor-grabbing shrink-0 touch-none transition-colors"
        {...(overlay ? {} : { ...attributes, ...listeners })}
        onClick={(e) => e.stopPropagation()}
      >
        <GripVertical className="h-3 w-3" />
      </button>

      {/* Color swatch – click to pick */}
      <div className="relative shrink-0" ref={pickerRef}>
        <button
          type="button"
          className="h-3 w-3 rounded-full ring-1 ring-white/20 hover:scale-125 active:scale-95 transition-transform shrink-0"
          style={{ backgroundColor: status.color }}
          onClick={(e) => { e.stopPropagation(); setPickerOpen((o) => !o); }}
        />
        {pickerOpen && (
          <div className="absolute top-5 left-0 z-50" onMouseDown={(e) => e.stopPropagation()}>
            <ColorPicker
              selectedColor={status.color}
              onSelect={(c) => { onUpdateColor(status.id, c); setPickerOpen(false); }}
            />
          </div>
        )}
      </div>

      {/* Editable name */}
      <input
        className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none text-foreground/90 placeholder:text-muted-foreground/30 focus:text-foreground min-w-0"
        value={status.name}
        onChange={(e) => onUpdateName(status.id, e.target.value)}
        onPointerDown={(e) => e.stopPropagation()}
        placeholder="Status name"
      />

      {/* Delete – only visible on hover */}
      <button
        type="button"
        className="opacity-0 group-hover:opacity-100 text-muted-foreground/30 hover:text-red-400 transition-all shrink-0"
        onClick={(e) => { e.stopPropagation(); onDelete(status.id); }}
      >
        <Trash2 className="h-3 w-3" />
      </button>
    </div>
  );
}

// ─── Main: CreateStatusForm ───────────────────────────────────────────────────

export const CreateStatusForm = observer(function CreateStatusForm({
  isOpen,
  onClose,
  spaceId,
  currentStatuses,
  onApplyChanges,
}: CreateStatusFormProps) {
  const rootStore = useWorkspaceRootStore();
  const statusMutations = useMemo(() => new StatusMutations(rootStore), [rootStore]);

  // "combined" (default) — shared workspace-level statuses + this space's own tagged ones, same
  // filter the board and task pickers use. "space" — just this space's own tagged statuses,
  // narrower, for when you only want to touch what's specific to this space.
  const [scope, setScope] = useState<"combined" | "space">("combined");

  // Plain read, not useMemo — this is a mobx-react-lite observer, which tracks observable reads
  // made directly during render (see FavoriteNodeList for the full rationale).
  const scopedStatuses = spaceId
    ? (scope === "space" ? rootStore.statusStore.getBySpace(spaceId) : rootStore.statusStore.getVisibleForSpace(spaceId))
    : rootStore.statusStore.all;
  const resolvedCurrentStatuses = currentStatuses ?? [...scopedStatuses]
    .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const [localStatuses, setLocalStatuses] = useState<Status[]>(() => resolvedCurrentStatuses);
  // Re-seed the working copy when the scope toggle changes — switching scope mid-edit is treated
  // as starting fresh on that narrower/wider view, same as reopening the dialog would.
  useEffect(() => {
    setLocalStatuses(resolvedCurrentStatuses);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scope]);
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const draggingItem = localStatuses.find((s) => s.id === draggingId) ?? null;
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const addInputRef = useRef<HTMLInputElement>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } })
  );

  const handleUpdateName = useCallback((id: string, name: string) => {
    setLocalStatuses((prev) => prev.map((s) => (s.id === id ? { ...s, name } : s)));
  }, []);

  const handleUpdateColor = useCallback((id: string, color: string) => {
    setLocalStatuses((prev) => prev.map((s) => (s.id === id ? { ...s, color } : s)));
  }, []);

  const handleDelete = useCallback((id: string) => {
    setLocalStatuses((prev) => prev.filter((s) => s.id !== id));
  }, []);

  const commitAdd = useCallback(() => {
    const trimmed = newName.trim();
    if (trimmed) {
      const newStatus: Status = {
        id: crypto.randomUUID(),
        spaceId,
        name: trimmed,
        color: PRESET_COLORS[Math.floor(Math.random() * PRESET_COLORS.length)],
        orderKey: "",
      };
      setLocalStatuses((prev) => [...prev, newStatus]);
    }
    setNewName("");
    setAdding(false);
  }, [newName, spaceId]);

  useEffect(() => {
    if (adding) addInputRef.current?.focus();
  }, [adding]);

  const handleDragStart = useCallback((e: DragStartEvent) => {
    setDraggingId(String(e.active.id));
  }, []);

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    setDraggingId(null);
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    setLocalStatuses((prev) => {
      const oldIdx = prev.findIndex((s) => s.id === active.id);
      const newIdx = prev.findIndex((s) => s.id === over.id);
      if (oldIdx === -1 || newIdx === -1) return prev;
      return arrayMove(prev, oldIdx, newIdx);
    });
  }, []);

  const handleSave = useCallback(() => {
    if (onApplyChanges) {
      // Local-preview mode — caller owns persistence (e.g. a template picker previewing a
      // starter set before the entity it belongs to even exists yet).
      onApplyChanges(localStatuses);
    } else {
      const payloads = buildStatusUpdatePayloads(localStatuses, resolvedCurrentStatuses);
      statusMutations.updateBatch(payloads).catch((err) => {
        console.error("Failed to save workflow statuses", err);
        toast.error("Failed to update statuses. Your changes have been reverted.");
      });
    }
    onClose();
  }, [localStatuses, resolvedCurrentStatuses, statusMutations, onApplyChanges, onClose]);

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) onClose(); }}>
      <DialogContent
        key={isOpen ? "open" : "closed"}
        className="max-w-120 w-full bg-background border border-border/30 text-foreground p-0 rounded-xl overflow-hidden shadow-2xl"
        showCloseButton={false}
      >
        {/* Header */}
        <DialogHeader className="flex flex-row items-center justify-between px-5 py-3 border-b border-border/20 bg-card/30 backdrop-blur-sm shrink-0">
          <div>
            <DialogTitle className="text-sm font-black text-foreground/90 tracking-tight">
              Workflow Manager
            </DialogTitle>
            <p className="text-[10px] text-muted-foreground/50 font-medium mt-0.5">
              Drag to reorder · Click color to change
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            title="Close"
            className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer shrink-0"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </DialogHeader>

        {/* Scope toggle — only meaningful with a space context */}
        {spaceId && (
          <div className="flex items-center gap-1 mx-4 mt-3 rounded-md border border-border/30 bg-card/60 p-1 shrink-0">
            <button
              type="button"
              onClick={() => setScope("combined")}
              className={cn(
                "flex-1 h-6 rounded text-[10px] font-semibold transition-colors cursor-pointer",
                scope === "combined" ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
              )}
            >
              Workspace + This Space
            </button>
            <button
              type="button"
              onClick={() => setScope("space")}
              className={cn(
                "flex-1 h-6 rounded text-[10px] font-semibold transition-colors cursor-pointer",
                scope === "space" ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
              )}
            >
              This Space Only
            </button>
          </div>
        )}

        {/* Flat, ordered status list */}
        <DndContext
          sensors={sensors}
          collisionDetection={pointerAwareCollisionDetection}
          modifiers={[restrictToVerticalAxis]}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <div className="flex flex-col gap-1.5 p-4 max-h-100 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/6 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/12 [&::-webkit-scrollbar-track]:bg-transparent">
            <SortableContext items={localStatuses.map((s) => s.id)} strategy={verticalListSortingStrategy}>
              {localStatuses.map((s) => (
                <StatusCard
                  key={s.id}
                  status={s}
                  onUpdateName={handleUpdateName}
                  onUpdateColor={handleUpdateColor}
                  onDelete={handleDelete}
                />
              ))}
            </SortableContext>

            {/* Add status row */}
            {adding ? (
              <div className="flex items-center gap-1.5 h-8 px-2 rounded-md border border-border/50 bg-card/80 shrink-0">
                <div className="h-2.5 w-2.5 rounded-full shrink-0 bg-muted-foreground/25" />
                <input
                  ref={addInputRef}
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  placeholder="Status name…"
                  className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none text-foreground/90 placeholder:text-muted-foreground/30 min-w-0"
                  onKeyDown={(e) => {
                    if (e.key === "Enter") commitAdd();
                    if (e.key === "Escape") { setAdding(false); setNewName(""); }
                  }}
                  onBlur={commitAdd}
                />
                <button
                  type="button"
                  className="text-emerald-400/70 hover:text-emerald-400 transition-colors shrink-0"
                  onMouseDown={(e) => { e.preventDefault(); commitAdd(); }}
                >
                  <Check className="h-3 w-3" />
                </button>
              </div>
            ) : (
              <button
                type="button"
                className="flex items-center gap-1.5 h-8 px-2 rounded-md border border-dashed border-border/25 text-muted-foreground/30 hover:text-muted-foreground/70 hover:border-border/50 hover:bg-white/2 transition-all shrink-0"
                onClick={() => { setAdding(true); setNewName(""); }}
              >
                <Plus className="h-3 w-3" />
                <span className="text-[10px] font-semibold">Add status</span>
              </button>
            )}
          </div>

          {createPortal(
            <DragOverlay dropAnimation={null}>
              {draggingItem ? (
                <StatusCard
                  status={draggingItem}
                  onUpdateName={() => {}}
                  onUpdateColor={() => {}}
                  onDelete={() => {}}
                  overlay
                />
              ) : null}
            </DragOverlay>,
            document.body
          )}
        </DndContext>

        {/* Footer */}
        <div className="flex items-center justify-between px-5 py-2.5 border-t border-border/15 bg-card/20 backdrop-blur-sm shrink-0">
          <span className="text-[9px] text-muted-foreground/25 font-medium">
            {localStatuses.length} total statuses
          </span>
          <button
            type="button"
            onClick={handleSave}
            className="flex items-center gap-1.5 h-7 px-3 rounded-md bg-primary/90 hover:bg-primary text-primary-foreground text-[11px] font-bold transition-all active:scale-95 shadow-sm"
          >
            <Check className="h-3 w-3" />
            Save
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
});

// ─── Build payloads ────────────────────────────────────────────────────────────

function buildStatusUpdatePayloads(
  localStatuses: Status[],
  resolvedCurrentStatuses: Status[]
): StatusUpdateValue[] {
  const originalStatuses = resolvedCurrentStatuses || [];
  const newIds = new Set(localStatuses.map((s) => s.id));
  const payloads: StatusUpdateValue[] = [];

  for (const s of originalStatuses) {
    if (!newIds.has(s.id)) {
      payloads.push({
        id: s.id,
        name: s.name,
        color: s.color,
        orderKey: null,
        action: RowAction.Delete,
      });
    }
  }

  for (let idx = 0; idx < localStatuses.length; idx++) {
    const s = localStatuses[idx];
    const isNew = !originalStatuses.some((orig) => orig.id === s.id);

    const prevKey = idx > 0 ? localStatuses[idx - 1].orderKey || null : null;
    const nextKey = idx < localStatuses.length - 1 ? localStatuses[idx + 1].orderKey || null : null;
    const orderKey = fractionalBetween(prevKey, nextKey);

    payloads.push({
      id: s.id,
      name: s.name,
      color: s.color,
      orderKey,
      spaceId: s.spaceId ?? null,
      action: isNew ? RowAction.Create : RowAction.Update,
    });
  }

  return payloads;
}

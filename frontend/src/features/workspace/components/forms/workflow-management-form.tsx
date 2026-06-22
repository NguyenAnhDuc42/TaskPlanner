import { useState, useRef, useCallback, useMemo, useEffect } from "react";
import { Plus, Trash2, Check, GripVertical } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  DndContext,
  closestCenter,
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { StatusCategory } from "@/types/status-category";
import type { Status } from "@/types/status";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useUpdateWorkflowStatuses } from "@/features/workspace/api";
import { RowAction } from "@/types/row-action";
import type { StatusUpdatePayload } from "@/features/workspace/api";
import { fractionalBetween } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";
import { createPortal } from "react-dom";

// ─── Types ────────────────────────────────────────────────────────────────────

interface CreateStatusFormProps {
  isOpen: boolean;
  onClose: () => void;
  workflowId?: string;
  currentStatuses?: Status[];
  onApplyChanges?: (statuses: Status[]) => void;
}

// ─── Constants ────────────────────────────────────────────────────────────────

const PRESET_COLORS = [
  "#ef4444", "#f97316", "#f59e0b", "#eab308",
  "#22c55e", "#10b981", "#06b6d4", "#3b82f6",
  "#6366f1", "#8b5cf6", "#ec4899", "#64748b",
];

const CATEGORY_CONFIG: Record<StatusCategory, { label: string; accent: string; glow: string; dot: string }> = {
  [StatusCategory.NotStarted]: {
    label: "Not Started",
    accent: "border-zinc-700/60 bg-zinc-800/40",
    glow: "shadow-zinc-900/60",
    dot: "bg-zinc-500",
  },
  [StatusCategory.Active]: {
    label: "Active",
    accent: "border-blue-500/30 bg-blue-950/20",
    glow: "shadow-blue-950/60",
    dot: "bg-blue-400",
  },
  [StatusCategory.Done]: {
    label: "Done",
    accent: "border-emerald-500/30 bg-emerald-950/20",
    glow: "shadow-emerald-950/60",
    dot: "bg-emerald-400",
  },
  [StatusCategory.Closed]: {
    label: "Closed",
    accent: "border-rose-500/20 bg-rose-950/10",
    glow: "shadow-rose-950/60",
    dot: "bg-rose-400/70",
  },
};

const CATEGORY_ORDER: StatusCategory[] = [
  StatusCategory.NotStarted,
  StatusCategory.Active,
  StatusCategory.Done,
  StatusCategory.Closed,
];

// ─── Color Picker ─────────────────────────────────────────────────────────────

function ColorPicker({
  selectedColor,
  onSelect,
}: {
  selectedColor: string;
  onSelect: (c: string) => void;
}) {
  const customRef = useRef<HTMLInputElement>(null);
  return (
    <div className="p-2 bg-popover border border-border/40 rounded-lg shadow-2xl w-[176px] flex flex-wrap gap-1.5">
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
          onClick={() => onSelect(c)}
        />
      ))}
      {/* Custom color button */}
      <button
        type="button"
        className="h-5 w-5 rounded-full shrink-0 bg-gradient-to-br from-red-500 via-blue-500 to-emerald-500 hover:scale-110 active:scale-95 transition-all ring-1 ring-white/10"
        onClick={() => customRef.current?.click()}
        title="Custom color"
      >
        <input
          ref={customRef}
          type="color"
          value={selectedColor}
          onChange={(e) => onSelect(e.target.value)}
          className="opacity-0 w-0 h-0 absolute"
        />
      </button>
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
        "group relative flex items-center gap-2 h-8 px-2 rounded-md border bg-card/60 hover:bg-card/90 border-border/30 hover:border-border/60 transition-all duration-150 select-none",
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

// ─── Category Column ──────────────────────────────────────────────────────────

function CategoryColumn({
  category,
  statuses,
  onUpdateName,
  onUpdateColor,
  onDelete,
  onAdd,
}: {
  category: StatusCategory;
  statuses: Status[];
  workflowId: string;
  onUpdateName: (id: string, name: string) => void;
  onUpdateColor: (id: string, color: string) => void;
  onDelete: (id: string) => void;
  onAdd: (category: StatusCategory, name: string) => void;
}) {
  const cfg = CATEGORY_CONFIG[category];
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const commitAdd = useCallback(() => {
    const trimmed = newName.trim();
    if (trimmed) onAdd(category, trimmed);
    setNewName("");
    setAdding(false);
  }, [newName, category, onAdd]);

  useEffect(() => {
    if (adding) inputRef.current?.focus();
  }, [adding]);

  return (
    <div
      className={cn(
        "flex flex-col gap-2 rounded-xl border p-3 min-w-[220px] flex-1 shadow-sm",
        cfg.accent,
        cfg.glow
      )}
    >
      {/* Column header */}
      <div className="flex items-center justify-between mb-1 shrink-0">
        <div className="flex items-center gap-1.5">
          <span className={cn("h-2 w-2 rounded-full shrink-0", cfg.dot)} />
          <span className="text-[10px] font-black uppercase tracking-[0.12em] text-foreground/60">
            {cfg.label}
          </span>
          <span className="text-[9px] font-bold text-muted-foreground/35 font-mono">
            {statuses.length}
          </span>
        </div>
        <button
          type="button"
          className="h-4 w-4 flex items-center justify-center rounded text-muted-foreground/30 hover:text-foreground hover:bg-white/[0.06] transition-all"
          onClick={() => { setAdding(true); setNewName(""); }}
        >
          <Plus className="h-3 w-3" />
        </button>
      </div>

      {/* Sortable status list */}
      <SortableContext items={statuses.map((s) => s.id)} strategy={verticalListSortingStrategy}>
        <div className="flex flex-col gap-1.5 flex-1">
          {statuses.map((s) => (
            <StatusCard
              key={s.id}
              status={s}
              onUpdateName={onUpdateName}
              onUpdateColor={onUpdateColor}
              onDelete={onDelete}
            />
          ))}
        </div>
      </SortableContext>

      {/* Add status row */}
      {adding ? (
        <div className="flex items-center gap-1.5 h-8 px-2 rounded-md border border-border/50 bg-card/80 shrink-0">
          <div className="h-2.5 w-2.5 rounded-full shrink-0 bg-muted-foreground/25" />
          <input
            ref={inputRef}
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
          className="flex items-center gap-1.5 h-8 px-2 rounded-md border border-dashed border-border/25 text-muted-foreground/30 hover:text-muted-foreground/70 hover:border-border/50 hover:bg-white/[0.02] transition-all shrink-0"
          onClick={() => { setAdding(true); setNewName(""); }}
        >
          <Plus className="h-3 w-3" />
          <span className="text-[10px] font-semibold">Add status</span>
        </button>
      )}
    </div>
  );
}

// ─── Main: CreateStatusForm ───────────────────────────────────────────────────

export function CreateStatusForm({
  isOpen,
  onClose,
  workflowId,
  currentStatuses,
  onApplyChanges,
}: CreateStatusFormProps) {
  const { workspaceId: currentWorkspaceId } = useWorkspace();
  const { mutate: updateStatuses } = useUpdateWorkflowStatuses();
  const allStatuses = useSelector(statusSelectors.selectAll);

  const resolvedCurrentStatuses = useMemo(() => {
    if (currentStatuses) return currentStatuses;
    if (!workflowId) return [];
    return allStatuses
      .filter((s: Status) => s.workflowId?.toLowerCase() === workflowId.toLowerCase())
      .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
  }, [currentStatuses, allStatuses, workflowId]);

  const [localStatuses, setLocalStatuses] = useState<Status[]>(() => resolvedCurrentStatuses);
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const draggingItem = localStatuses.find((s) => s.id === draggingId) ?? null;

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } })
  );

  const grouped = useMemo(
    () =>
      CATEGORY_ORDER.reduce<Record<StatusCategory, Status[]>>((acc, cat) => {
        acc[cat] = localStatuses.filter((s) => s.category === cat);
        return acc;
      }, {} as Record<StatusCategory, Status[]>),
    [localStatuses]
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

  const handleAdd = useCallback((category: StatusCategory, name: string) => {
    const newStatus: Status = {
      id: crypto.randomUUID(),
      workflowId: workflowId ?? "",
      name,
      color: PRESET_COLORS[Math.floor(Math.random() * 8)],
      category,
      orderKey: "",
    };
    setLocalStatuses((prev) => [...prev, newStatus]);
  }, [workflowId]);

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
    if (workflowId) {
      const { payloads, clonedStatuses } = buildStatusUpdatePayloads(localStatuses, resolvedCurrentStatuses);
      updateStatuses({
        workflowId,
        workspaceId: currentWorkspaceId,
        statuses: payloads,
        optimisticStatuses: clonedStatuses,
      });
    } else {
      onApplyChanges?.(localStatuses);
    }
    onClose();
  }, [workflowId, localStatuses, resolvedCurrentStatuses, updateStatuses, currentWorkspaceId, onApplyChanges, onClose]);

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) onClose(); }}>
      <DialogContent
        key={isOpen ? "open" : "closed"}
        className="max-w-[960px] w-full bg-background border border-border/30 text-foreground p-0 rounded-xl overflow-hidden shadow-2xl"
        showCloseButton={false}
      >
        {/* Header */}
        <DialogHeader className="flex flex-row items-center justify-between px-5 py-3 border-b border-border/20 bg-card/30 backdrop-blur-sm shrink-0">
          <div>
            <DialogTitle className="text-sm font-black text-foreground/90 tracking-tight">
              Workflow Manager
            </DialogTitle>
            <p className="text-[10px] text-muted-foreground/50 font-medium mt-0.5">
              Drag to reorder · Click color to change · Changes save on close
            </p>
          </div>
          <button
            type="button"
            onClick={handleSave}
            className="flex items-center gap-1.5 h-7 px-3 rounded-md bg-primary/90 hover:bg-primary text-primary-foreground text-[11px] font-bold transition-all active:scale-95 shadow-sm"
          >
            <Check className="h-3 w-3" />
            Save
          </button>
        </DialogHeader>

        {/* Horizontal columns */}
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          modifiers={[restrictToVerticalAxis]}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <div className="flex gap-3 p-4 overflow-x-auto [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.06] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.12] [&::-webkit-scrollbar-track]:bg-transparent">
            {CATEGORY_ORDER.map((cat) => (
              <CategoryColumn
                key={cat}
                category={cat}
                statuses={grouped[cat]}
                workflowId={workflowId ?? ""}
                onUpdateName={handleUpdateName}
                onUpdateColor={handleUpdateColor}
                onDelete={handleDelete}
                onAdd={handleAdd}
              />
            ))}
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

        {/* Footer legend */}
        <div className="flex items-center gap-4 px-5 py-2.5 border-t border-border/15 bg-card/20 backdrop-blur-sm shrink-0">
          {CATEGORY_ORDER.map((cat) => {
            const cfg = CATEGORY_CONFIG[cat];
            return (
              <div key={cat} className="flex items-center gap-1.5">
                <span className={cn("h-1.5 w-1.5 rounded-full", cfg.dot)} />
                <span className="text-[9px] font-semibold text-muted-foreground/40 uppercase tracking-wide">
                  {cfg.label}
                </span>
                <span className="text-[8px] font-mono text-muted-foreground/25">
                  {grouped[cat].length}
                </span>
              </div>
            );
          })}
          <span className="ml-auto text-[9px] text-muted-foreground/25 font-medium">
            {localStatuses.length} total statuses
          </span>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ─── Build payloads (unchanged logic) ─────────────────────────────────────────

function buildStatusUpdatePayloads(
  localStatuses: Status[],
  resolvedCurrentStatuses: Status[]
): { payloads: StatusUpdatePayload[]; clonedStatuses: Status[] } {
  const originalStatuses = resolvedCurrentStatuses || [];
  const newIds = new Set(localStatuses.map((s) => s.id));
  const payloads: StatusUpdatePayload[] = [];

  for (const s of originalStatuses) {
    if (!newIds.has(s.id)) {
      payloads.push({
        id: s.id,
        name: s.name,
        color: s.color,
        category: s.category,
        previousOrderKey: null,
        nextOrderKey: null,
        action: RowAction.Delete,
      });
    }
  }

  const clonedStatuses = localStatuses.map((s) => ({ ...s }));

  const categoryGroups: Record<string, Status[]> = {};
  for (const s of clonedStatuses) {
    if (!categoryGroups[s.category]) categoryGroups[s.category] = [];
    categoryGroups[s.category].push(s);
  }

  for (const s of clonedStatuses) {
    const isNew = !originalStatuses.some((orig) => orig.id === s.id);
    const catGroup = categoryGroups[s.category] || [];
    const idx = catGroup.findIndex((item) => item.id === s.id);

    const prevKey = idx > 0 ? catGroup[idx - 1].orderKey || null : null;
    const nextKey = idx < catGroup.length - 1 ? catGroup[idx + 1].orderKey || null : null;

    s.orderKey = fractionalBetween(prevKey, nextKey);

    payloads.push({
      id: s.id,
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

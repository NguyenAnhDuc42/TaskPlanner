/**
 * Generic drag-to-reorder list. Handles DndContext + SortableContext boilerplate.
 * Use alongside SortableListItem for each draggable child.
 *
 * Supports vertical (default) and horizontal layouts.
 * onReorder receives the reordered array plus the id of the item that was actually dragged —
 * caller handles the save mutation. The id is required, not optional: diffing old vs. new array
 * to guess which item moved is unreliable (a front-to-back move shifts every index, so "first
 * index that differs" always lands on index 0 — the item that slid INTO that slot, not the one
 * that was actually dragged there).
 */
import { useCallback } from "react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import { createPortal } from "react-dom";
import { useState } from "react";
import {
  SortableContext,
  verticalListSortingStrategy,
  rectSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis, restrictToHorizontalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";

interface SortableListProps<T extends { id: string }> {
  items: T[];
  onReorder: (newItems: T[], draggedId: string) => void;
  direction?: "vertical" | "horizontal";
  activationDistance?: number;
  className?: string;
  children: React.ReactNode;
  /** Optional overlay rendered inside a DragOverlay portal while dragging */
  renderOverlay?: (draggedId: string) => React.ReactNode;
}

export function SortableList<T extends { id: string }>({
  items,
  onReorder,
  direction = "vertical",
  activationDistance = 5,
  className,
  children,
  renderOverlay,
}: SortableListProps<T>) {
  const [draggedId, setDraggedId] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: activationDistance } })
  );

  const handleDragStart = useCallback(({ active }: DragStartEvent) => {
    setDraggedId(active.id as string);
  }, []);

  const handleDragEnd = useCallback(({ active, over }: DragEndEvent) => {
    setDraggedId(null);
    if (!over || active.id === over.id) return;
    const oldIdx = items.findIndex(i => i.id === active.id);
    const newIdx = items.findIndex(i => i.id === over.id);
    if (oldIdx !== -1 && newIdx !== -1) onReorder(arrayMove(items, oldIdx, newIdx), active.id as string);
  }, [items, onReorder]);

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={pointerAwareCollisionDetection}
      modifiers={[direction === "vertical" ? restrictToVerticalAxis : restrictToHorizontalAxis]}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <SortableContext
        items={items.map(i => i.id)}
        strategy={direction === "vertical" ? verticalListSortingStrategy : rectSortingStrategy}
      >
        <div className={className}>{children}</div>
      </SortableContext>

      {renderOverlay && createPortal(
        <DragOverlay dropAnimation={null}>
          {draggedId ? renderOverlay(draggedId) : null}
        </DragOverlay>,
        document.body
      )}
    </DndContext>
  );
}

// ── Per-item wrapper ────────────────────────────────────────────────────────

interface SortableListItemProps {
  id: string;
  className?: string;
  /** Render function receives drag handle props + isDragging flag */
  children: (props: {
    dragHandleProps: Record<string, unknown>;
    isDragging: boolean;
  }) => React.ReactNode;
}

export function SortableListItem({ id, children, className }: SortableListItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });
  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
  };
  // Wrap in a div so dnd-kit can measure the element directly — no ref threading needed
  return (
    <div ref={setNodeRef} style={style} className={className}>
      {children({ dragHandleProps: { ...attributes, ...listeners }, isDragging })}
    </div>
  );
}

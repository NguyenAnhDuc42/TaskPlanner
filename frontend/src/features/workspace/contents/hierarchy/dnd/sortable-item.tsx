import React from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";

export function SortableItem({ id, data, children }: Readonly<{ id: string; data: Record<string, unknown>; children: React.ReactNode }>) {
  const { canCreateContent } = useWorkspaceRole();
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging
  } = useSortable({ id, data });

  const style = {
    transform: CSS.Translate.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    scale: isDragging ? 0.98 : 1,
    zIndex: isDragging ? 50 : 1,
    position: 'relative' as const,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...(canCreateContent ? listeners : {})}>
      {children}
    </div>
  );
}

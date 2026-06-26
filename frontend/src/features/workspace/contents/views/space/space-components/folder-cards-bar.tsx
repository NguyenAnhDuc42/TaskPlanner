import { useState, useCallback, useRef } from "react";
import { useDispatch } from "react-redux";
import { folderSlice } from "@/store/entityStore";
import type { AppDispatch } from "@/store";
import { createPortal } from "react-dom";
import { useNavigate } from "@tanstack/react-router";
import {
  DndContext,
  DragOverlay,
  MeasuringStrategy,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import { restrictToHorizontalAxis, restrictToParentElement } from "@dnd-kit/modifiers";
import {
  SortableContext,
  horizontalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Plus, X } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";
import { useCreateFolderMutation, useDeleteFolderMutation, useBatchMoveItemsMutation } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import type { FolderRecord } from "@/types/projects";

// ── Single folder chip ──────────────────────────────────────────────────────

function FolderChip({
  folder,
  taskCount,
  onClick,
  onDelete,
}: {
  folder: FolderRecord;
  taskCount: number;
  onClick: () => void;
  onDelete: () => void;
}) {
  return (
    <div className="flex items-center gap-1 pl-2.5 pr-1 py-1 rounded-md border border-border/30 bg-card hover:border-border/60 hover:bg-muted/20 transition-all shrink-0 group/chip">
      <button type="button" onClick={onClick} className="flex items-center gap-1.5 cursor-pointer min-w-0">
        <DynamicIcon name={folder.icon || "Folder"} size={11} color={folder.color || "#6366f1"} />
        <span className="text-[11px] font-medium text-foreground/80 group-hover/chip:text-foreground truncate max-w-[100px]">
          {folder.name}
        </span>
        <span className="text-[9px] font-mono text-muted-foreground/40 bg-muted/40 px-1 rounded-sm shrink-0">
          {taskCount}
        </span>
      </button>
      <button
        type="button"
        onClick={e => { e.stopPropagation(); onDelete(); }}
        className="opacity-0 group-hover/chip:opacity-100 transition-opacity text-muted-foreground/40 hover:text-destructive/80 p-0.5 shrink-0"
      >
        <X className="h-2.5 w-2.5" />
      </button>
    </div>
  );
}

// ── Sortable wrapper around FolderChip ─────────────────────────────────────

function SortableFolderChip({
  folder,
  taskCount,
  onClick,
  onDelete,
}: {
  folder: FolderRecord;
  taskCount: number;
  onClick: () => void;
  onDelete: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: folder.id });

  return (
    <div
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition: isDragging ? undefined : transition,
        opacity: isDragging ? 0 : 1,
      }}
      {...attributes}
      {...listeners}
      className="cursor-grab active:cursor-grabbing touch-none"
    >
      <FolderChip folder={folder} taskCount={taskCount} onClick={onClick} onDelete={onDelete} />
    </div>
  );
}

// ── Main bar ────────────────────────────────────────────────────────────────

interface FolderCardsBarProps {
  spaceId: string;
  workspaceId: string;
  folders: FolderRecord[];
  folderTaskCounts: Record<string, number>;
}

export function FolderCardsBar({ spaceId, workspaceId, folders, folderTaskCounts }: FolderCardsBarProps) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const [createFolder] = useCreateFolderMutation();
  const [deleteFolder] = useDeleteFolderMutation();
  const [batchMove] = useBatchMoveItemsMutation();

  const dispatch = useDispatch<AppDispatch>();
  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const submittedRef = useRef(false);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const handleDragStart = useCallback(({ active }: DragStartEvent) => {
    setDraggedId(active.id as string);
  }, []);

  const handleDragEnd = useCallback(({ active, over }: DragEndEvent) => {
    setDraggedId(null);
    if (!over || active.id === over.id) return;
    const oldIdx = folders.findIndex(f => f.id === active.id);
    const newIdx = folders.findIndex(f => f.id === over.id);
    if (oldIdx === -1 || newIdx === -1) return;
    const reordered = arrayMove(folders, oldIdx, newIdx);
    // Optimistic: update order keys in store immediately so board re-renders at once
    dispatch(folderSlice.actions.upsertMany(
      reordered.map((f, idx) => ({ id: f.id, orderKey: String(idx + 1).padStart(8, "0") }))
    ));
    batchMove({
      workspaceId,
      command: {
        folders: reordered.map((f, idx) => ({
          itemId: f.id,
          targetParentId: spaceId,
          newOrderKey: String(idx + 1).padStart(8, "0"),
        })),
      },
    });
  }, [folders, workspaceId, spaceId, batchMove, dispatch]);

  const handleCreate = useCallback(() => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const name = newName.trim();
    setIsCreating(false);
    setNewName("");
    if (name) {
      createFolder({ workspaceId, body: { spaceId, name, color: "#6366f1", icon: "Folder" } })
        .unwrap()
        .catch(err => toast.error(extractErrorMessage(err, "Failed to create folder")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  }, [newName, workspaceId, spaceId, createFolder]);

  const draggedFolder = folders.find(f => f.id === draggedId);

  return (
    <div className="flex items-center gap-1.5 px-2 py-1.5 border-b border-border/15 overflow-x-auto shrink-0 [&::-webkit-scrollbar]:hidden">
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        modifiers={[restrictToHorizontalAxis, restrictToParentElement]}
        measuring={{ droppable: { strategy: MeasuringStrategy.Always } }}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <SortableContext items={folders.map(f => f.id)} strategy={horizontalListSortingStrategy}>
          <div className="flex items-center gap-1.5">
            {folders.map(folder => (
              <SortableFolderChip
                key={folder.id}
                folder={folder}
                taskCount={folderTaskCounts[folder.id] ?? 0}
                onClick={() => navigate({
                  to: "/workspaces/$workspaceId/folders/$folderId",
                  params: { workspaceId, folderId: folder.id },
                })}
                onDelete={() =>
                  deleteFolder({ workspaceId, folderId: folder.id })
                    .unwrap()
                    .catch(err => toast.error(extractErrorMessage(err, "Failed to delete folder")))
                }
              />
            ))}
          </div>
        </SortableContext>

        {createPortal(
          <DragOverlay dropAnimation={null}>
            {draggedFolder && (
              <div className="rotate-2 scale-105 opacity-90 cursor-grabbing shadow-lg">
                <FolderChip
                  folder={draggedFolder}
                  taskCount={folderTaskCounts[draggedFolder.id] ?? 0}
                  onClick={() => {}}
                  onDelete={() => {}}
                />
              </div>
            )}
          </DragOverlay>,
          document.body
        )}
      </DndContext>

      {/* Create */}
      {isCreating ? (
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md border border-primary/40 bg-primary/5 shrink-0">
          <DynamicIcon name="Folder" size={11} color="#6366f1" />
          <input
            autoFocus
            type="text"
            value={newName}
            onChange={e => setNewName(e.target.value)}
            onKeyDown={e => {
              if (e.key === "Enter") e.currentTarget.blur();
              if (e.key === "Escape") { setIsCreating(false); setNewName(""); }
            }}
            onBlur={handleCreate}
            className="text-[11px] font-medium bg-transparent border-none outline-none w-24 text-foreground"
            placeholder="Name..."
          />
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setIsCreating(true)}
          className="flex items-center gap-1 px-2 py-1 rounded-md text-muted-foreground/40 hover:text-muted-foreground hover:bg-muted/30 transition-all shrink-0 cursor-pointer"
        >
          <Plus className="h-3 w-3" />
          <span className="text-[10px] font-medium">New folder</span>
        </button>
      )}
    </div>
  );
}

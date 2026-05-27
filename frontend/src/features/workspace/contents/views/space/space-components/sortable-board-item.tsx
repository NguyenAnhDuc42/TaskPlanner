import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Priority } from "@/types/priority";
import { TaskItem } from "@/features/workspace/contents/layer-detail/components/items/task-item";
import { FolderItem } from "@/features/workspace/contents/layer-detail/components/items/folder-item";
import type { BoardItem } from "../space-api";

export function SortableBoardItem({
  item,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
}: {
  item: BoardItem;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: item.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.3 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="outline-none"
    >
      {item.__type === "task" ? (
        <TaskItem
          task={item}
          onClick={() => onTaskClick(item.id)}
          onPriorityChange={(itemId, p) => onPriorityChange(itemId, "task", p)}
        />
      ) : (
        <FolderItem
          folder={item}
          onClick={() => onFolderClick(item.id)}
          onPriorityChange={(itemId, p) => onPriorityChange(itemId, "folder", p)}
        />
      )}
    </div>
  );
}

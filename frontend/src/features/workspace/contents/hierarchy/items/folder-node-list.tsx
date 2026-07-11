import { useEffect, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { FolderMutations } from "@/mutations/folder.mutations";
import { extractErrorMessage } from "@/types/api-error";
import { toast } from "sonner";
import { Folder as FolderIcon } from "lucide-react";
import { FolderNodeItem } from "@/features/workspace/contents/hierarchy/items/folder-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";

interface NodeFoldersListProps {
  spaceId: string;
  isExpanded: boolean;
  isCreating?: boolean;
  onCreatingChange?: (creating: boolean) => void;
}

export const NodeFoldersList = observer(function NodeFoldersList({
  spaceId,
  isExpanded,
  isCreating = false,
  onCreatingChange,
}: NodeFoldersListProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const folderMutations = useMemo(() => new FolderMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [newName, setNewName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    if (!isCreating) return;
    let raf2 = 0;
    const raf1 = requestAnimationFrame(() => {
      raf2 = requestAnimationFrame(() => inputRef.current?.focus());
    });
    return () => { cancelAnimationFrame(raf1); cancelAnimationFrame(raf2); };
  }, [isCreating]);

  if (!isExpanded) return null;

  const folders = rootStore.folderStore.getBySpace(spaceId).sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  const handleCreate = () => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const name = newName.trim();
    onCreatingChange?.(false);
    setNewName("");
    if (name) {
      folderMutations.create({ spaceId, name, color: "#6366f1", icon: "Folder" })
        .catch((err) => toast.error(extractErrorMessage(err, "Failed to create folder")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  };

  return (
    <SortableContext
      items={folders.map((f) => `folder-${f.id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {folders.map((f) => (
          <FolderNodeItem key={f.id} folderId={f.id} spaceId={spaceId} />
        ))}

        {isCreating && (
          <div className="flex items-center gap-1.5 px-1 py-0.5 rounded-md border border-primary/40 bg-primary/5 mb-px">
            <div className="w-5 h-5 flex items-center justify-center shrink-0">
              <FolderIcon className="h-3.5 w-3.5" color="#6366f1" />
            </div>
            <input
              ref={inputRef}
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
                if (e.key === "Escape") { onCreatingChange?.(false); setNewName(""); }
              }}
              onBlur={handleCreate}
              placeholder="Folder name..."
              className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none text-foreground placeholder:text-muted-foreground/40"
            />
          </div>
        )}
      </div>
    </SortableContext>
  );
});

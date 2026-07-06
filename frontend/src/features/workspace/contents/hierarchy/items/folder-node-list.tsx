import { observer } from "mobx-react-lite";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { FolderNodeItem } from "@/features/workspace/contents/hierarchy/items/folder-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";

export const NodeFoldersList = observer(function NodeFoldersList({
  spaceId,
  isExpanded,
}: {
  spaceId: string;
  isExpanded: boolean;
}) {
  const rootStore = useWorkspaceRootStore();

  if (!isExpanded) return null;

  const folders = rootStore.folderStore.getBySpace(spaceId).sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

  return (
    <SortableContext
      items={folders.map((f) => `folder-${f.id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {folders.map((f) => (
          <FolderNodeItem key={f.id} folderId={f.id} spaceId={spaceId} />
        ))}
      </div>
    </SortableContext>
  );
});

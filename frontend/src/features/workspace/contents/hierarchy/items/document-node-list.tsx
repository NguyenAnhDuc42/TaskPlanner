import { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { Plus } from "lucide-react";
import { DndContext, DragOverlay } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { DocumentMutations } from "@/mutations/document.mutations";
import { useHierarchyDnd } from "../dnd/use-hierarchy-dnd";
import { DragOverlayRow } from "../dnd/drag-overlay-row";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { DocumentNodeItem } from "./document-node-item";
import { DocumentGhostRow } from "./document-ghost-row";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

interface DocumentNodeListProps {
  spaceId: string;
  activeDocumentId: string;
  onSelect: (documentId: string) => void;
}

export const DocumentNodeList = observer(function DocumentNodeList({ spaceId, activeDocumentId, onSelect }: DocumentNodeListProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { canCreateContent } = useWorkspaceRole();
  const documentMutations = useMemo(() => new DocumentMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const { sensors, handleDragStart, handleDragEnd, activeItem } = useHierarchyDnd();

  const roots = rootStore.documentStore.getRootsBySpace(spaceId);
  const [isCreatingRoot, setIsCreatingRoot] = useState(false);

  const commitCreateRoot = (name: string) => {
    setIsCreatingRoot(false);
    documentMutations
      .create({ spaceId, name, parentDocumentId: null })
      .then((created) => onSelect(created.id))
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to create document")));
  };

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={pointerAwareCollisionDetection}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      modifiers={[restrictToVerticalAxis]}
    >
      <div className="flex flex-col min-w-fit">
        <SortableContext items={roots.map((d) => `document-${d.id}`)} strategy={verticalListSortingStrategy}>
          {roots.map((doc) => (
            <DocumentNodeItem
              key={doc.id}
              documentId={doc.id}
              activeDocumentId={activeDocumentId}
              onSelect={onSelect}
              onRequestSiblingCreate={() => setIsCreatingRoot(true)}
            />
          ))}
        </SortableContext>

        {isCreatingRoot ? (
          <DocumentGhostRow onCommit={commitCreateRoot} onCancel={() => setIsCreatingRoot(false)} />
        ) : (
          canCreateContent && (
            <button
              type="button"
              onClick={() => setIsCreatingRoot(true)}
              className="flex items-center gap-1.5 px-1 py-0.5 mt-0.5 rounded-md text-muted-foreground/50 hover:text-foreground hover:bg-muted/50 transition-colors cursor-pointer"
            >
              <div className="w-5 h-5 flex items-center justify-center shrink-0">
                <Plus className="h-3.5 w-3.5" />
              </div>
              <span className="text-[11px] font-semibold">Add page</span>
            </button>
          )
        )}
      </div>

      <DragOverlay adjustScale={false} zIndex={1000} dropAnimation={null}>
        {activeItem && activeItem.type === EntityLayerConst.ProjectDocument ? (
          <div className="opacity-80 scale-105 transition-transform pointer-events-none shadow-2xl rounded-sm overflow-hidden ring-1 ring-primary/20">
            <DragOverlayRow item={activeItem} />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
});

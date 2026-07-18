import { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { ChevronRight, MoreVertical, Plus, Trash2, FilePlus } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { DocumentMutations } from "@/mutations/document.mutations";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { SortableItem } from "../dnd/sortable-item";
import { DeleteConfirmationDialog, EditFieldsSubmenu } from "../hierarchy-components/context-menus/shared";
import { DocumentGhostRow } from "./document-ghost-row";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

interface DocumentNodeItemProps {
  documentId: string;
  activeDocumentId: string;
  onSelect: (documentId: string) => void;
  onRequestSiblingCreate: () => void;
  depth?: number;
}

export const DocumentNodeItem = observer(function DocumentNodeItem({
  documentId,
  activeDocumentId,
  onSelect,
  onRequestSiblingCreate,
  depth = 0,
}: DocumentNodeItemProps) {
  const { workspaceId } = useWorkspace();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const documentMutations = useMemo(() => new DocumentMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const document = rootStore.documentStore.getById(documentId);
  const children = rootStore.documentStore.getChildren(documentId);
  const hasChildren = children.length > 0;

  const [isOpen, setIsOpen] = useLocalStorage(`doc-tree-open:${workspaceId}:${documentId}`, false);
  const [isCreatingChild, setIsCreatingChild] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  if (!document) return null;

  const isActive = documentId === activeDocumentId;
  const icon = document.icon || "FileText";
  const color = document.color || "#ffffff";

  const handleCreateSubPage = () => {
    setIsOpen(true);
    setIsCreatingChild(true);
  };

  const commitCreateChild = (name: string) => {
    setIsCreatingChild(false);
    documentMutations
      .create({ spaceId: document.spaceId, name, parentDocumentId: document.id })
      .then((created) => onSelect(created.id))
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to create document")));
  };

  const handleDelete = () => {
    documentMutations
      .delete(documentId)
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to delete document")));
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        <Item onSelect={() => onRequestSiblingCreate()} className="gap-2 cursor-pointer">
          <Plus className="h-3.5 w-3.5" /> New page
        </Item>
        <Item onSelect={handleCreateSubPage} className="gap-2 cursor-pointer">
          <FilePlus className="h-3.5 w-3.5" /> New sub-page
        </Item>
        <EditFieldsSubmenu
          isContext={isContext}
          name={document.name}
          icon={icon}
          color={color}
          onRename={(name) => {
            if (!name.trim()) return;
            documentMutations
              .update(documentId, { name: name.trim() })
              .catch((err) => toast.error(extractErrorMessage(err, "Failed to rename document")));
          }}
          onIconColorChange={(icon, color) =>
            documentMutations
              .update(documentId, { icon, color })
              .catch((err) => toast.error(extractErrorMessage(err, "Failed to update document icon")))
          }
        />
        <Separator />
        <Item onSelect={() => setIsDeleteOpen(true)} variant="destructive" className="gap-2 cursor-pointer">
          <Trash2 className="h-3.5 w-3.5" /> Delete
        </Item>
      </>
    );
  };

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <SortableItem
        id={`document-${document.id}`}
        data={{
          ...document,
          type: EntityLayerConst.ProjectDocument,
          id: document.id,
          parentId: document.parentDocumentId ?? null,
          spaceId: document.spaceId,
          orderKey: document.orderKey,
        }}
      >
        <ContextMenu>
          <ContextMenuTrigger asChild>
            <div
              className={cn(
                "group flex items-center px-1 py-0.5 rounded-md mb-px border",
                isActive
                  ? "bg-primary/10 text-primary border-primary/25"
                  : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
              )}
              style={{ paddingLeft: 4 + depth * 14 }}
            >
              <button
                type="button"
                className="relative flex items-center justify-center w-5 h-5 shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1 transition-none"
                onClick={(e) => {
                  if (!hasChildren) return;
                  e.stopPropagation();
                  setIsOpen(!isOpen);
                }}
              >
                <DynamicIcon
                  name={icon}
                  size={14}
                  color={color}
                  className={cn("absolute transition-none", hasChildren && "group-hover/icon:opacity-0")}
                />
                {hasChildren && (
                  <ChevronRight
                    className={cn(
                      "h-3.5 w-3.5 absolute opacity-0 text-muted-foreground group-hover/icon:opacity-100 transition-none",
                      isOpen && "rotate-90",
                    )}
                  />
                )}
              </button>

              <button
                type="button"
                onClick={() => onSelect(document.id)}
                className="flex-1 text-left text-[11px] font-semibold truncate min-w-0"
              >
                {document.name}
              </button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild onPointerDown={(e) => e.stopPropagation()}>
                  <button
                    type="button"
                    className="h-4 w-4 flex items-center justify-center rounded-sm opacity-0 group-hover:opacity-100 hover:bg-background/60 transition-colors shrink-0"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreVertical className="h-3.5 w-3.5" />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="start" side="right" className="w-48" onCloseAutoFocus={(e) => e.preventDefault()}>
                  {renderMenuItems(false)}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </ContextMenuTrigger>
          <ContextMenuContent className="w-48" onCloseAutoFocus={(e) => e.preventDefault()}>
            {renderMenuItems(true)}
          </ContextMenuContent>
        </ContextMenu>
      </SortableItem>

      {(hasChildren || isCreatingChild) && (
        <CollapsibleContent className="overflow-hidden">
          <SortableContext items={children.map((c) => `document-${c.id}`)} strategy={verticalListSortingStrategy}>
            {children.map((child) => (
              <DocumentNodeItem
                key={child.id}
                documentId={child.id}
                activeDocumentId={activeDocumentId}
                onSelect={onSelect}
                onRequestSiblingCreate={() => setIsCreatingChild(true)}
                depth={depth + 1}
              />
            ))}
          </SortableContext>
          {isCreatingChild && (
            <DocumentGhostRow
              depth={depth + 1}
              onCommit={commitCreateChild}
              onCancel={() => setIsCreatingChild(false)}
            />
          )}
        </CollapsibleContent>
      )}

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Document"
        description={`Are you sure you want to delete "${document.name}"? This will also delete all of its sub-pages and cannot be undone.`}
        onConfirm={handleDelete}
      />
    </Collapsible>
  );
});

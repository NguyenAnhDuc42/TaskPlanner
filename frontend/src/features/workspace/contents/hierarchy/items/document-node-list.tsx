import { useCallback, useMemo, useRef, useState } from "react";
import { observer } from "mobx-react-lite";
import { useVirtualizer } from "@tanstack/react-virtual";
import { Plus } from "lucide-react";
import { DndContext, DragOverlay } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy, type SortingStrategy } from "@dnd-kit/sortable";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { DocumentMutations } from "@/mutations/document.mutations";
import { useHierarchyDnd } from "../dnd/use-hierarchy-dnd";
import { DragOverlayRow } from "../dnd/drag-overlay-row";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { DocumentNodeItem } from "./document-node-item";
import { DocumentGhostRow } from "./document-ghost-row";
import type { DocumentRecord } from "@/types/projects";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

interface DocumentNodeListProps {
  spaceId: string;
  activeDocumentId: string;
  onSelect: (documentId: string) => void;
}

type FlatRow =
  | { kind: "document"; documentId: string; depth: number; hasChildren: boolean; isOpen: boolean; parentId: string | null }
  | { kind: "ghost"; depth: number; parentId: string | null };

const ROW_HEIGHT = 24;
const NO_SHIFT = { x: 0, y: 0, scaleX: 1, scaleY: 1 };

// Siblings aren't necessarily adjacent in the flattened/virtualized row array — an expanded
// sibling's own descendants sit between it and the next sibling. dnd-kit's stock
// verticalListSortingStrategy shifts every row between activeIndex and overIndex by raw array
// position with no concept of hierarchy, so dragging past an expanded branch would visually drag
// its unrelated nested rows along too. This scopes the shift math to only the rows that are
// actually siblings of the one being dragged, by filtering down to same-parent rects and
// remapping indices before delegating to the stock strategy's math.
function createDepthAwareSortingStrategy(getParentId: (index: number) => string | null | undefined): SortingStrategy {
  return (args) => {
    const { activeIndex, index, overIndex, rects } = args;
    const activeParent = getParentId(activeIndex);
    if (index !== activeIndex && getParentId(index) !== activeParent) {
      return NO_SHIFT;
    }

    const siblingIndices: number[] = [];
    for (let i = 0; i < rects.length; i++) {
      if (getParentId(i) === activeParent) siblingIndices.push(i);
    }
    const remappedActiveIndex = siblingIndices.indexOf(activeIndex);
    const remappedIndex = siblingIndices.indexOf(index);
    const remappedOverIndex = siblingIndices.indexOf(overIndex);
    // overIndex outside this sibling group means the drag is currently hovering a row under a
    // different parent (a reparent, not a same-level reorder) — no clean in-place shift to
    // preview among just this level, so skip the live preview and let it settle after drop.
    if (remappedActiveIndex === -1 || remappedIndex === -1 || remappedOverIndex === -1) {
      return NO_SHIFT;
    }

    return verticalListSortingStrategy({
      ...args,
      activeIndex: remappedActiveIndex,
      overIndex: remappedOverIndex,
      index: remappedIndex,
      rects: siblingIndices.map((i) => rects[i]),
    });
  };
}

function readExpandedIds(storageKey: string): Set<string> {
  try {
    const raw = globalThis.window?.localStorage.getItem(storageKey);
    return raw ? new Set(JSON.parse(raw) as string[]) : new Set();
  } catch {
    return new Set();
  }
}

// The tree used to be fully recursive components (one <DocumentNodeItem> mounting its own
// <CollapsibleContent> of children, all the way down), each with its own useSortable() drag
// registration. With enough pages in a space that gets slow: every visible row — every root page
// plus every currently-expanded descendant — mounted a full row component (context menus,
// dropdowns, a fresh DocumentMutations instance, and dnd-kit's per-row measurement/registration)
// simultaneously, uncapped. This version flattens the tree into a row array (recursing only into
// *expanded* nodes, so collapsed subtrees cost nothing) and virtualizes rendering of that array,
// so cost scales with what's actually on screen, not with total document count.
export const DocumentNodeList = observer(function DocumentNodeList({ spaceId, activeDocumentId, onSelect }: DocumentNodeListProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { workspaceId } = useWorkspace();
  const { canCreateContent } = useWorkspaceRole();
  const documentMutations = useMemo(() => new DocumentMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const { sensors, handleDragStart, handleDragEnd, activeItem } = useHierarchyDnd();

  const storageKey = `doc-tree-expanded:${workspaceId}:${spaceId}`;
  const [expandedIds, setExpandedIds] = useState<Set<string>>(() => readExpandedIds(storageKey));
  const [creatingUnder, setCreatingUnder] = useState<{ parentId: string | null } | null>(null);

  const persistExpanded = useCallback((next: Set<string>) => {
    setExpandedIds(next);
    try {
      globalThis.window?.localStorage.setItem(storageKey, JSON.stringify([...next]));
    } catch {
      // ignore
    }
  }, [storageKey]);

  const toggleOpen = useCallback((documentId: string) => {
    const next = new Set(expandedIds);
    if (next.has(documentId)) {
      next.delete(documentId);
      // A pending ghost row under a node that's collapsing would no longer be reachable.
      setCreatingUnder((prev) => (prev?.parentId === documentId ? null : prev));
    } else {
      next.add(documentId);
    }
    persistExpanded(next);
  }, [expandedIds, persistExpanded]);

  const expandNode = useCallback((documentId: string) => {
    if (expandedIds.has(documentId)) return;
    persistExpanded(new Set(expandedIds).add(documentId));
  }, [expandedIds, persistExpanded]);

  const roots = rootStore.documentStore.getRootsBySpace(spaceId);

  const flatRows = useMemo(() => {
    const rows: FlatRow[] = [];
    const visit = (doc: DocumentRecord, depth: number) => {
      const children = rootStore.documentStore.getChildren(doc.id);
      const isOpen = expandedIds.has(doc.id);
      rows.push({
        kind: "document",
        documentId: doc.id,
        depth,
        hasChildren: children.length > 0,
        isOpen,
        parentId: doc.parentDocumentId ?? null,
      });
      if (isOpen) {
        for (const child of children) visit(child, depth + 1);
        if (creatingUnder && creatingUnder.parentId === doc.id) {
          rows.push({ kind: "ghost", depth: depth + 1, parentId: doc.id });
        }
      }
    };
    for (const root of roots) visit(root, 0);
    if (creatingUnder && creatingUnder.parentId === null) {
      rows.push({ kind: "ghost", depth: 0, parentId: null });
    }
    return rows;
  }, [roots, expandedIds, creatingUnder, rootStore.documentStore]);

  const scrollContainerRef = useRef<HTMLDivElement | null>(null);
  const rowVirtualizer = useVirtualizer({
    count: flatRows.length,
    getScrollElement: () => scrollContainerRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: 12,
    // Without this, react-virtual keys rows by array index — so when a reorder changes which
    // document occupies a given index, React sees "same slot, new props" instead of "this
    // document moved," and reuses the same DOM/SortableItem instance for a different id. dnd-kit
    // then re-registers useSortable() with the new id on that reused instance, carrying over
    // stale transition state, which is what shows up as an unwanted slide instead of an instant
    // settle. Keying by the row's actual identity lets React (and dnd-kit) correctly track each
    // document as the same element moving between slots.
    getItemKey: (index) => {
      const row = flatRows[index];
      return row.kind === "document" ? row.documentId : `ghost-${row.parentId ?? "root"}`;
    },
  });

  const virtualItems = rowVirtualizer.getVirtualItems();
  const sortableRows = useMemo(
    () =>
      virtualItems
        .map((vi) => flatRows[vi.index])
        .filter((row): row is Extract<FlatRow, { kind: "document" }> => row.kind === "document"),
    [virtualItems, flatRows],
  );
  const sortableIds = useMemo(() => sortableRows.map((row) => `document-${row.documentId}`), [sortableRows]);
  const sortingStrategy = useMemo(
    () => createDepthAwareSortingStrategy((i) => sortableRows[i]?.parentId),
    [sortableRows],
  );

  const commitCreate = (name: string, parentId: string | null) => {
    setCreatingUnder(null);
    documentMutations
      .create({ spaceId, name, parentDocumentId: parentId })
      .then((created) => onSelect(created.id))
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to create document")));
  };

  const requestSiblingCreate = useCallback((parentId: string | null) => {
    if (parentId) expandNode(parentId);
    setCreatingUnder({ parentId });
  }, [expandNode]);

  const requestChildCreate = useCallback((documentId: string) => {
    expandNode(documentId);
    setCreatingUnder({ parentId: documentId });
  }, [expandNode]);

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={pointerAwareCollisionDetection}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      modifiers={[restrictToVerticalAxis]}
    >
      <div
        ref={scrollContainerRef}
        className="h-full min-w-fit overflow-y-auto px-1.5 pb-3 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20"
      >
        <SortableContext items={sortableIds} strategy={sortingStrategy}>
          <div style={{ height: rowVirtualizer.getTotalSize(), position: "relative" }}>
            {virtualItems.map((vi) => {
              const row = flatRows[vi.index];
              return (
                <div
                  key={vi.key}
                  data-index={vi.index}
                  ref={rowVirtualizer.measureElement}
                  style={{ position: "absolute", top: 0, left: 0, right: 0, transform: `translateY(${vi.start}px)` }}
                >
                  {row.kind === "ghost" ? (
                    <DocumentGhostRow
                      depth={row.depth}
                      onCommit={(name) => commitCreate(name, row.parentId)}
                      onCancel={() => setCreatingUnder(null)}
                    />
                  ) : (
                    <DocumentNodeItem
                      documentId={row.documentId}
                      depth={row.depth}
                      hasChildren={row.hasChildren}
                      isOpen={row.isOpen}
                      activeDocumentId={activeDocumentId}
                      onSelect={onSelect}
                      onToggleOpen={toggleOpen}
                      onRequestSiblingCreate={() => requestSiblingCreate(row.parentId)}
                      onRequestChildCreate={() => requestChildCreate(row.documentId)}
                    />
                  )}
                </div>
              );
            })}
          </div>
        </SortableContext>

        {creatingUnder === null && canCreateContent && (
          <button
            type="button"
            onClick={() => requestSiblingCreate(null)}
            className="flex items-center gap-1.5 px-1 py-0.5 mt-0.5 rounded-md text-muted-foreground/50 hover:text-foreground hover:bg-muted/50 transition-colors cursor-pointer"
          >
            <div className="w-5 h-5 flex items-center justify-center shrink-0">
              <Plus className="h-3.5 w-3.5" />
            </div>
            <span className="text-[11px] font-semibold">Add page</span>
          </button>
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

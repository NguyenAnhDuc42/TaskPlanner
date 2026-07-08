import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { reaction } from "mobx";
import { BlockType } from "@/types/block-type";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { DocumentBlockMutations } from "@/mutations/document-block.mutations";
import { api } from "@/lib/api-client";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";

type AnyBlock = {
  id: string;
  type: string;
  props?: Record<string, unknown>;
  [k: string]: unknown;
};

function getBlockType(
  type: string,
  props?: Record<string, unknown>,
): BlockType {
  switch (type) {
    case "heading":
      if (props?.level === 2) return BlockType.Heading2;
      if (props?.level === 3) return BlockType.Heading3;
      return BlockType.Heading1;
    case "bulletListItem":
      return BlockType.BulletList;
    case "numberedListItem":
      return BlockType.OrderedList;
    case "checkListItem":
      return BlockType.TaskItem;
    case "codeBlock":
      return BlockType.CodeBlock;
    case "image":
      return BlockType.Image;
    case "video":
      return BlockType.Video;
    case "file":
      return BlockType.File;
    default:
      return BlockType.Paragraph;
  }
}

function hashBlock(block: AnyBlock): string {
  const { id: _id, ...rest } = block;
  void _id;
  return JSON.stringify(rest);
}

export function useBlockEditorSync(documentId: string) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const documentBlockMutations = useMemo(
    () => new DocumentBlockMutations(rootStore, syncEngine),
    [rootStore, syncEngine],
  );

  const { scheduleFlush } = useDebouncedFlush(syncEngine, 4000);

  const [ready, setReady] = useState<{
    content: AnyBlock[] | undefined;
    version: number;
  } | null>(null);

  const snapshotRef = useRef<Map<string, string>>(new Map());
  const isInitRef = useRef(false);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isSavingRef = useRef(false);
  const latestRef = useRef<AnyBlock[] | null>(null);

  useEffect(() => {
    isInitRef.current = false;
    snapshotRef.current = new Map();

    if (!documentId) return;

    let cancelled = false;

    const syncFromStore = () => {
      const dbBlocks = rootStore.documentBlockStore.getByDocument(documentId);
      const blocks: AnyBlock[] = [];
      const snapshot = new Map<string, string>();

      for (const db of dbBlocks) {
        let parsed: AnyBlock;
        try {
          parsed = JSON.parse(db.content) as AnyBlock;
        } catch {
          parsed = { id: db.id, type: "paragraph", props: {}, content: [], children: [] };
        }
        if (parsed.type === "doc") {
          parsed = { id: db.id, type: "paragraph", props: {}, content: [], children: [] };
        }
        const block: AnyBlock = { ...parsed, id: db.id };
        blocks.push(block);
        snapshot.set(db.id, hashBlock(block));
      }

      if (!isInitRef.current) {
        isInitRef.current = true;
        snapshotRef.current = snapshot;
        setReady({ content: blocks.length > 0 ? blocks : undefined, version: 0 });
        return;
      }

      // Skip while a local save is in flight — that's this hook's own writes echoing back
      // through the store, not an external update, and we don't want to yank the editor's
      // content (and remount it via the `version` key) out from under an in-progress edit.
      if (saveTimerRef.current === null && !isSavingRef.current) {
        snapshotRef.current = snapshot;
        setReady(prev => ({
          content: blocks.length > 0 ? blocks : undefined,
          version: (prev?.version ?? 0) + 1,
        }));
      }
    };

    syncFromStore();

    const dispose = reaction(
      () => rootStore.documentBlockStore
        .getByDocument(documentId)
        .map((b) => `${b.id}:${b.type}:${b.content}:${b.orderKey}`)
        .join("|"),
      syncFromStore,
    );
    const context = `document_blocks:${documentId}`;
    (async () => {
      // Local-first hydration: read straight from IndexedDB before deciding whether a network
      // fetch is even needed. The in-memory store alone doesn't carry this across a fresh page
      // load — DocumentBlocks are intentionally excluded from Bootstrap (lazy per-document tier
      // only, see FRONTEND_SYNC_CONTEXT.md §1b), so without this the editor always waited on a
      // network round-trip even when the data was already cached locally.
      const cached = await rootStore.documentBlockDB!.getAllByDocument(documentId);
      if (cancelled) return;
      if (cached.length > 0) {
        for (const block of cached) {
          rootStore.documentBlockStore.upsert(block);
        }
      }

      const alreadyFetched = await rootStore.fetchedContextDB!.hasFetched(context);
      if (alreadyFetched || cancelled) return;

      try {
        const { data } = await api.get<DocumentBlockRecord[]>(`/documents/${documentId}/sync/blocks`);
        if (cancelled) return;
        for (const block of data) {
          rootStore.documentBlockStore.upsert(block);
        }
        await rootStore.documentBlockDB!.putMany(data);
        await rootStore.fetchedContextDB!.markFetched(context);
      } catch (err) {
        console.error(`Failed to fetch blocks for document ${documentId}:`, err);
      }
    })();

    return () => {
      cancelled = true;
      dispose();
    };
  }, [documentId, rootStore]);

  const performSave = useCallback(
    (blocks: AnyBlock[]) => {
      const prev = snapshotRef.current;
      const next = new Map<string, string>();
      const prevOrder = Array.from(prev.keys());

      type BlockOp =
        | { kind: "create"; id: string; type: BlockType; content: string; orderKey: string }
        | { kind: "update"; id: string; type: BlockType; content: string; orderKey: string }
        | { kind: "delete"; id: string };

      const pendingOps: BlockOp[] = [];

      blocks.forEach((block, index) => {
        const hash = hashBlock(block);
        const orderKey = String(index + 1).padStart(8, "0");
        next.set(block.id, hash);

        const isNew = !prev.has(block.id);
        const changed = prev.get(block.id) !== hash;
        const reordered = prevOrder.indexOf(block.id) !== index;

        if (isNew || changed || reordered) {
          const { id: _id, ...rest } = block;
          void _id;
          pendingOps.push({
            kind: isNew ? "create" : "update",
            id: block.id,
            type: getBlockType(block.type, block.props),
            content: JSON.stringify(rest),
            orderKey,
          });
        }
      });

      for (const prevId of prev.keys()) {
        if (!next.has(prevId)) {
          pendingOps.push({ kind: "delete", id: prevId });
        }
      }

      snapshotRef.current = next;
      if (pendingOps.length === 0) return;
      isSavingRef.current = true;
      Promise.all(
        pendingOps.map((op) => {
          if (op.kind === "create") {
            return documentBlockMutations.createLocal({ id: op.id, documentId, type: op.type, content: op.content, orderKey: op.orderKey });
          }
          if (op.kind === "update") {
            return documentBlockMutations.updateLocal(op.id, { type: op.type, content: op.content, orderKey: op.orderKey });
          }
          return documentBlockMutations.deleteLocal(op.id);
        }),
      )
        .then(() => scheduleFlush())
        .catch((err) => console.error("Failed to queue document block changes", err))
        .finally(() => { isSavingRef.current = false; });
    },
    [documentId, documentBlockMutations, scheduleFlush],
  );

  const handleUpdate = useCallback(
    (blocks: AnyBlock[]) => {
      latestRef.current = blocks;
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
      saveTimerRef.current = setTimeout(() => {
        performSave(blocks);
        saveTimerRef.current = null;
      }, 600);
    },
    [performSave],
  );

  useEffect(
    () => () => {
      if (saveTimerRef.current && latestRef.current) {
        clearTimeout(saveTimerRef.current);
        performSave(latestRef.current);
      }
    },
    [performSave],
  );

  return {
    initialContent: ready?.content,
    handleUpdate,
    isReady: ready !== null,
    version: ready?.version ?? 0,
  };
}

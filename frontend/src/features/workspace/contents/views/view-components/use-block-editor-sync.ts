import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { reaction } from "mobx";
import { BlockType } from "@/types/block-type";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { DocumentBlockMutations } from "@/mutations/document-block.mutations";

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
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const documentBlockMutations = useMemo(
    () => new DocumentBlockMutations(rootStore, syncEngine),
    [rootStore, syncEngine],
  );
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const [ready, setReady] = useState<{
    content: AnyBlock[] | undefined;
    version: number;
  } | null>(null);

  const snapshotRef = useRef<Map<string, string>>(new Map());
  const isInitRef = useRef(false);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isSavingRef = useRef(false);
  const latestRef = useRef<AnyBlock[] | null>(null);

  // DocumentBlock is now part of Bootstrap+Delta (like Task/Space/Folder), so
  // documentBlockStore is the source of truth from the moment the workspace loads — no legacy
  // fetch needed. A mobx reaction re-runs this whenever blocks for this document change,
  // whether from our own saves or a Delta from another client/tab.
  useEffect(() => {
    isInitRef.current = false;
    snapshotRef.current = new Map();

    if (!documentId) return;

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

    return reaction(
      () => rootStore.documentBlockStore
        .getByDocument(documentId)
        .map((b) => `${b.id}:${b.type}:${b.content}:${b.orderKey}`)
        .join("|"),
      syncFromStore,
    );
  }, [documentId, rootStore]);

  // Diffing stays the same as before (new/changed/deleted/reordered via snapshot hashes) — only
  // the destination changed. Each changed block becomes its own queued C/U/D transaction
  // (createLocal/updateLocal/deleteLocal — local store + IndexedDB + enqueue, no network), then
  // one shared debounced flush sends everything queued in a single POST /api/sync/batch call.
  // N block edits = N SyncEvents, but always exactly one HTTP round trip.
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

      // Set before any store write so the reaction above sees isSavingRef=true and skips —
      // otherwise our own optimistic upsert (which happens synchronously inside createLocal/
      // updateLocal, before their first await) would immediately trigger a false "external
      // update" re-init.
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
      }, 2000);
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

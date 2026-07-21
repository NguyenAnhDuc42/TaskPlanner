import { useState, useEffect, useLayoutEffect, useRef, useCallback, useMemo } from "react";
import { reaction } from "mobx";
import { BlockType } from "@/types/block-type";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { DocumentBlockMutations, type DocumentBlockBatchOp } from "@/mutations/document-block.mutations";
import { fractionalBetweenN, safeKey } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
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

function fnv1aMix(h: number, str: string): number {
  for (let i = 0; i < str.length; i++) {
    h ^= str.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return h;
}

export function hashJson(json: string): string {
  return (fnv1aMix(0x811c9dc5, json) >>> 0).toString(36);
}

function hashBlock(block: AnyBlock): string {
  const { id: _id, ...rest } = block;
  void _id;
  return hashJson(JSON.stringify(rest));
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
    documentId: string;
    content: AnyBlock[] | undefined;
    version: number;
  } | null>(null);

  const snapshotRef = useRef<Map<string, string>>(new Map());
  const isInitRef = useRef(false);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isSavingRef = useRef(false);
  // Tagged with the documentId it was captured for — see handleUpdate's comment. latestRef is a
  // single shared ref (not scoped per document), so without the tag, a flush triggered by one
  // document's cleanup/timer could pick up another document's content that got written here in
  // between, and save it under the wrong id.
  const latestRef = useRef<{ documentId: string; blocks: AnyBlock[] } | null>(null);
  const pendingStoreSyncRef = useRef(false);
  const syncFromStoreRef = useRef<(() => void) | null>(null);

  // BlockNoteView doesn't reliably re-subscribe its onChange callback on every render (the editor
  // instance is a persistent singleton reused across documents), so a final "blur" onChange from
  // the document being navigated away from can fire holding a stale closure — old documentId, old
  // handleUpdate/performSave — *after* this hook has already re-run for the new document. Since
  // snapshotRef/latestRef/saveTimerRef are shared mutable refs (not scoped per document), that
  // stale call's debounced save fires later using the new document's snapshot as its diff baseline,
  // producing bogus create/delete ops applied under the *old* document's id. This ref is updated
  // unconditionally on every render (always current) so handleUpdate can detect and drop any call
  // whose closed-over documentId no longer matches reality, instead of trusting the closure.
  const activeDocumentIdRef = useRef(documentId);
  useLayoutEffect(() => {
    activeDocumentIdRef.current = documentId;
  }, [documentId]);

  // Declared before the layoutEffect below (not grouped with handleUpdate further down) so that
  // effect's own cleanup can call it directly — see the comment there for why that matters.
  const performSave = useCallback(
    (blocks: AnyBlock[]) => {
      const prev = snapshotRef.current;
      const next = new Map<string, string>();

      const existingKeys = new Map(
        rootStore.documentBlockStore.getByDocument(documentId).map((b) => [b.id, b.orderKey]),
      );
      const hasLegacyKeys = Array.from(existingKeys.values()).some((k) => safeKey(k) === null);

      const assignedKeys: (string | null)[] = new Array(blocks.length).fill(null);
      if (hasLegacyKeys) {
        const fresh = fractionalBetweenN(null, null, blocks.length);
        for (let i = 0; i < blocks.length; i++) assignedKeys[i] = fresh[i];
      } else {
        let lastKept: string | null = null;
        for (let i = 0; i < blocks.length; i++) {
          const k = existingKeys.get(blocks[i].id) ?? null;
          if (k !== null && (lastKept === null || k > lastKept)) {
            assignedKeys[i] = k;
            lastKept = k;
          }
        }
        let i = 0;
        while (i < blocks.length) {
          if (assignedKeys[i] !== null) { i++; continue; }
          let runEnd = i;
          while (runEnd < blocks.length && assignedKeys[runEnd] === null) runEnd++;
          const prevKey = i > 0 ? assignedKeys[i - 1] : null;
          const nextKey = runEnd < blocks.length ? assignedKeys[runEnd] : null;
          const generated = fractionalBetweenN(prevKey, nextKey, runEnd - i);
          for (let j = i; j < runEnd; j++) assignedKeys[j] = generated[j - i];
          i = runEnd;
        }
      }

      const pendingOps: DocumentBlockBatchOp[] = [];

      blocks.forEach((block, index) => {
        const { id: _id, ...rest } = block;
        void _id;
        const json = JSON.stringify(rest);
        const hash = hashJson(json);
        next.set(block.id, hash);

        const orderKey = assignedKeys[index]!;
        const isNew = !prev.has(block.id);
        const contentChanged = prev.get(block.id) !== hash;
        const keyChanged = existingKeys.get(block.id) !== orderKey;

        if (isNew || contentChanged || keyChanged) {
          pendingOps.push({
            kind: isNew ? "create" : "update",
            id: block.id,
            type: getBlockType(block.type, block.props),
            content: json,
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
      documentBlockMutations
        .applyLocalBatch(documentId, pendingOps)
        .then(() => {
          // Locally reflect the edit immediately — real edits from *other* clients still only
          // refresh this on the next bootstrap, since block deltas don't currently touch the
          // owning document's own cached record. Good enough for "did I just edit this."
          rootStore.documentStore.update(documentId, { lastEditedAt: new Date().toISOString() });
          scheduleFlush();
        })
        .catch((err) => {
          console.error("Failed to queue document block changes", err);
          if (snapshotRef.current === next) snapshotRef.current = prev;
        })
        .finally(() => {
          isSavingRef.current = false;
          if (pendingStoreSyncRef.current) {
            pendingStoreSyncRef.current = false;
            syncFromStoreRef.current?.();
          }
        });
    },
    [documentId, documentBlockMutations, scheduleFlush, rootStore.documentBlockStore, rootStore.documentStore],
  );

  useLayoutEffect(() => {
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
        setReady({ documentId, content: blocks.length > 0 ? blocks : undefined, version: 0 });
        return;
      }

      if (saveTimerRef.current !== null || isSavingRef.current) {
        pendingStoreSyncRef.current = true;
        return;
      }

      const prevSnapshot = snapshotRef.current;
      const prevIds = Array.from(prevSnapshot.keys());
      const nextIds = Array.from(snapshot.keys());
      const identical =
        prevIds.length === nextIds.length &&
        nextIds.every((id, i) => prevIds[i] === id && prevSnapshot.get(id) === snapshot.get(id));
      if (identical) return;

      snapshotRef.current = snapshot;
      setReady(prev => ({
        documentId,
        content: blocks.length > 0 ? blocks : undefined,
        version: (prev?.documentId === documentId ? prev.version : 0) + 1,
      }));
    };
    syncFromStoreRef.current = syncFromStore;

    const dispose = reaction(
      () => {
        const list = rootStore.documentBlockStore.getByDocument(documentId);
        let h = 0x811c9dc5;
        for (const b of list) {
          h = fnv1aMix(h, b.id);
          h = fnv1aMix(h, b.type);
          h = fnv1aMix(h, b.content);
          h = fnv1aMix(h, b.orderKey);
          h = Math.imul(h ^ 0x1f, 0x01000193);
        }
        return `${list.length}:${(h >>> 0).toString(36)}`;
      },
      syncFromStore,
    );
    const context = `document_blocks:${documentId}`;

    if (rootStore.documentBlockStore.getByDocument(documentId).length > 0) {
      syncFromStore();
    }

    (async () => {
      const cached = await rootStore.documentBlockDB!.getAllByDocument(documentId);
      if (cancelled) return;
      if (cached.length > 0) {
        rootStore.documentBlockStore.upsertMany(cached);
        syncFromStore(); // idempotent — isInitRef guards
      }

      const alreadyFetched = await rootStore.fetchedContextDB!.hasFetched(context);
      if ((alreadyFetched && cached.length > 0) || cancelled) return;

      for (let attempt = 1; attempt <= 3 && !cancelled; attempt++) {
        try {
          const { data } = await api.get<DocumentBlockRecord[]>(`/documents/${documentId}/sync/blocks`);
          if (cancelled) return;
          rootStore.documentBlockStore.upsertMany(data);
          await rootStore.documentBlockDB!.putMany(data);
          await rootStore.fetchedContextDB!.markFetched(context);
          break;
        } catch (err) {
          if (attempt === 3) {
            console.error(`Failed to fetch blocks for document ${documentId} after ${attempt} attempts:`, err);
            break;
          }
          await new Promise((resolve) => setTimeout(resolve, 1000 * attempt));
        }
      }

      if (!cancelled) syncFromStore();
    })();

    return () => {
      cancelled = true;
      dispose();
      syncFromStoreRef.current = null;
      pendingStoreSyncRef.current = false;

      // Flush a still-pending debounced save for the document we're leaving, using its own
      // snapshot, BEFORE the next run of this effect resets snapshotRef for the new document.
      // This used to live in a separate `useEffect(() => () => {...}, [performSave])` meant as an
      // unmount-only safety net — but performSave gets a new reference every time documentId
      // changes, so that cleanup actually fired on every document switch, not just true unmount.
      // By the time it ran, snapshotRef had already been reset (synchronously, in this same
      // layout effect) for the *new* document, so the old document's blocks were compared against
      // the wrong snapshot and read as entirely new — queuing a bogus create for every block, on
      // every switch. Doing the flush right here, in this effect's own cleanup, guarantees it
      // runs against the correct (still current) snapshot before anything gets reset.
      //
      // The documentId tag check matters just as much: latestRef is a single ref shared across
      // every document this hook ever tracks, not scoped per document. A stale/deferred onChange
      // from BlockNoteView (it doesn't reliably re-subscribe onChange per document — see
      // activeDocumentIdRef above) can write a *different* document's content into latestRef
      // between this document's own edits and this cleanup running. Without the tag, this cleanup
      // would flush that other document's content under this document's id.
      if (saveTimerRef.current && latestRef.current?.documentId === documentId) {
        clearTimeout(saveTimerRef.current);
        saveTimerRef.current = null;
        performSave(latestRef.current.blocks);
        latestRef.current = null;
      }
    };
  }, [documentId, rootStore, performSave]);

  const handleUpdate = useCallback(
    (blocks: AnyBlock[]) => {
      // A stale onChange from the document we just navigated away from — see activeDocumentIdRef's
      // comment above. Silently drop it: acting on it would diff this document's stale content
      // against whatever document is now current's snapshot, producing bogus ops under this id.
      if (documentId !== activeDocumentIdRef.current) return;
      latestRef.current = { documentId, blocks };
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
      saveTimerRef.current = setTimeout(() => {
        saveTimerRef.current = null;
        // Re-check here, not just at schedule time above: the user can switch documents during
        // this 200ms window, which makes an until-now-valid scheduled save stale by the time it
        // actually fires. The cleanup flush (in the layout effect below) already handles saving
        // this document's real pending edit on the way out — this timer firing after that point
        // would just be a duplicate/stale write against a snapshot that's moved on.
        if (documentId !== activeDocumentIdRef.current) return;
        performSave(blocks);
        if (latestRef.current?.documentId === documentId) latestRef.current = null;
      }, 200);
    },
    [performSave, documentId],
  );

  useEffect(() => {
    const onHidden = () => {
      if (document.visibilityState !== "hidden") return;
      if (saveTimerRef.current && latestRef.current?.documentId === documentId) {
        clearTimeout(saveTimerRef.current);
        saveTimerRef.current = null;
        performSave(latestRef.current.blocks);
        latestRef.current = null;
      }
      syncEngine.flushQueue().catch(() => {});
    };
    document.addEventListener("visibilitychange", onHidden);
    window.addEventListener("pagehide", onHidden);
    return () => {
      document.removeEventListener("visibilitychange", onHidden);
      window.removeEventListener("pagehide", onHidden);
    };
  }, [performSave, syncEngine, documentId]);

  useEffect(() => {
    if (!documentId) return;
    rootStore.documentBlockStore.retainDocument(documentId);
    return () => rootStore.documentBlockStore.releaseDocument(documentId);
  }, [documentId, rootStore.documentBlockStore]);

  const readyForCurrentDocument = ready?.documentId === documentId ? ready : null;

  return {
    initialContent: readyForCurrentDocument?.content,
    handleUpdate,
    isReady: readyForCurrentDocument !== null,
    version: readyForCurrentDocument?.version ?? 0,
  };
}

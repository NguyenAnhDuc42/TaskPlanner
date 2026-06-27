import { useState, useEffect, useRef, useCallback } from "react";
import {
  useGetDocumentBlocksQuery,
  useUpdateDocumentBlocksMutation,
} from "./document-api";
import { BlockType } from "@/types/block-type";

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
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { id, ...rest } = block;
  void id;
  return JSON.stringify(rest);
}

export function useBlockEditorSync(documentId: string) {
  const { data: dbBlocks, isSuccess } = useGetDocumentBlocksQuery(documentId, {
    skip: !documentId,
  });
  const [updateBlocks] = useUpdateDocumentBlocksMutation();

  // version increments when external SignalR update forces editor remount
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
    if (!isSuccess) return;

    const blocks: AnyBlock[] = [];
    const snapshot = new Map<string, string>();

    for (const db of dbBlocks ?? []) {
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
      // First load
      isInitRef.current = true;
      snapshotRef.current = snapshot;
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setReady({ content: blocks.length > 0 ? blocks : undefined, version: 0 });
      return;
    }

    // External update (SignalR) — only re-init if user has no pending unsaved changes and no save in flight
    if (saveTimerRef.current === null && !isSavingRef.current) {
      snapshotRef.current = snapshot;
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setReady(prev => ({
        content: blocks.length > 0 ? blocks : undefined,
        version: (prev?.version ?? 0) + 1,
      }));
    }
  }, [isSuccess, dbBlocks]);

  const performSave = useCallback(
    (blocks: AnyBlock[]) => {
      const prev = snapshotRef.current;
      const next = new Map<string, string>();
      const upserts: import("./document-api").DocumentBlockValue[] = [];
      const deletes: import("./document-api").DocumentBlockValue[] = [];
      const prevOrder = Array.from(prev.keys());

      blocks.forEach((block, index) => {
        const hash = hashBlock(block);
        const orderKey = String(index + 1).padStart(8, "0");
        next.set(block.id, hash);

        const isNew = !prev.has(block.id);
        const changed = prev.get(block.id) !== hash;
        const reordered = prevOrder.indexOf(block.id) !== index;

        if (isNew || changed || reordered) {
          // Strip id from content — backend stores content without the id field
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { id: _id, ...rest } = block;
          upserts.push({
            id: block.id,
            content: JSON.stringify(rest),
            orderKey,
            blockType: getBlockType(block.type, block.props),
            isDeleted: false,
          });
        }
      });

      for (const prevId of prev.keys()) {
        if (!next.has(prevId)) {
          deletes.push({
            id: prevId,
            content: "",
            orderKey: "",
            blockType: BlockType.Paragraph,
            isDeleted: true,
          });
        }
      }

      snapshotRef.current = next;
      const changes = [...upserts, ...deletes];
      if (changes.length > 0) {
        isSavingRef.current = true;
        updateBlocks({ documentId, blocks: changes }).finally(() => {
          isSavingRef.current = false;
        });
      }
    },
    [documentId, updateBlocks],
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

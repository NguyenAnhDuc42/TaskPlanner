import { useState, useEffect, useRef, useCallback } from "react";
import {
  useGetDocumentBlocksQuery,
  useSaveDocumentBlocksMutation,
  type BlockSaveItem,
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
  const { id: _id, ...rest } = block;
  void _id;
  return JSON.stringify(rest);
}

export function useBlockEditorSync(documentId: string) {
  const { data: dbBlocks, isSuccess } = useGetDocumentBlocksQuery(documentId, {
    skip: !documentId,
  });
  const [saveBlocks] = useSaveDocumentBlocksMutation();

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
      isInitRef.current = true;
      snapshotRef.current = snapshot;
      setReady({ content: blocks.length > 0 ? blocks : undefined, version: 0 });
      return;
    }

    // External update (SignalR) — only re-init if no pending unsaved changes
    if (saveTimerRef.current === null && !isSavingRef.current) {
      snapshotRef.current = snapshot;
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
      const changes: BlockSaveItem[] = [];
      const prevOrder = Array.from(prev.keys());

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
          changes.push({
            id: block.id,
            type: getBlockType(block.type, block.props),
            content: JSON.stringify(rest),
            orderKey,
            isDeleted: false,
          });
        }
      });

      for (const prevId of prev.keys()) {
        if (!next.has(prevId)) {
          changes.push({
            id: prevId,
            type: BlockType.Paragraph,
            content: "",
            orderKey: "",
            isDeleted: true,
          });
        }
      }

      snapshotRef.current = next;
      if (changes.length > 0) {
        isSavingRef.current = true;
        saveBlocks({ documentId, blocks: changes }).finally(() => {
          isSavingRef.current = false;
        });
      }
    },
    [documentId, saveBlocks],
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

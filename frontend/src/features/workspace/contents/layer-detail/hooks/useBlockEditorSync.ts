import { useEffect, useMemo, useRef } from "react";
import { useDocumentBlocks, useUpdateDocumentBlocks } from "../views/document-api";
import type { DocumentBlockDto, DocumentBlockValue, TiptapDoc } from "../types/document-types";
import { BlockType } from "@/types/block-type";

export function useBlockEditorSync(documentId: string) {
  const { data: blocks } = useDocumentBlocks(documentId);
  const { mutate: updateBlocks } = useUpdateDocumentBlocks();
  
  const initialBlockIds = useRef<Set<string>>(new Set());
  const saveTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Convert flat DB blocks to Tiptap JSON
  const initialContent = useMemo<TiptapDoc>(() => {
    if (!blocks || blocks.length === 0) return { type: "doc", content: [] };
    
    const firstBlock = blocks[0];
    if (firstBlock && firstBlock.content) {
      try {
        return JSON.parse(firstBlock.content) as TiptapDoc;
      } catch (e) {
        console.error("Failed to parse document JSON", e);
      }
    }
    
    return { type: "doc", content: [] };
  }, [blocks]);

  const latestJsonRef = useRef<any>(null);

  const performSave = (json: any) => {
    const contentStr = JSON.stringify(json);
    const firstBlock = blocks?.[0];
    
    const blocksToUpdate: DocumentBlockValue[] = [
      {
        id: firstBlock?.id,
        content: contentStr,
        orderKey: "A",
        blockType: BlockType.Paragraph,
        isDeleted: false
      }
    ];

    // Mark all other blocks as deleted to clean up the DB
    const deletedBlocks: DocumentBlockValue[] = [];
    blocks?.forEach((b: DocumentBlockDto) => {
      if (b.id !== firstBlock?.id) {
        deletedBlocks.push({
          id: b.id,
          content: "",
          orderKey: "",
          blockType: BlockType.Paragraph,
          isDeleted: true
        });
      }
    });

    const allBlocks = [...blocksToUpdate, ...deletedBlocks];
    
    if (allBlocks.length > 0) {
      updateBlocks({ documentId, blocks: allBlocks });
    }
  };

  // Handle the debounced save
  const handleUpdate = (json: any) => {
    if (saveTimeoutRef.current) clearTimeout(saveTimeoutRef.current);
    latestJsonRef.current = json;

    saveTimeoutRef.current = setTimeout(() => {
      performSave(json);
      saveTimeoutRef.current = null;
    }, 1000);
  };

  // Save on unmount if there are pending changes
  useEffect(() => {
    return () => {
      if (saveTimeoutRef.current && latestJsonRef.current) {
        clearTimeout(saveTimeoutRef.current);
        performSave(latestJsonRef.current);
      }
    };
  }, [blocks, documentId]);

  return {
    initialContent,
    handleUpdate
  };
}
